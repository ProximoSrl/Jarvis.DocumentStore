using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp;
using AngleSharp.Html;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class ContentFormatBuilder
    {
        private readonly ContentFilterManager _filterManager;

        public ContentFormatBuilder(ContentFilterManager filterManager)
        {
            _filterManager = filterManager;
        }

        public ContentFormatBuilderResult CreateFromTikaPlain(String tikaFullContent)
        {
            if (String.IsNullOrEmpty(tikaFullContent)) 
                return new ContentFormatBuilderResult() 
                {
                    Content = DocumentContent.NullContent,
                    SanitizedTikaContent = tikaFullContent,
                };

            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);

            //Just get the DOM representation
            var doc = context.OpenAsync(req => req.Content(tikaFullContent)).Result;

            var allMeta = doc.QuerySelectorAll("meta");

            var meta = allMeta.SelectMany(x => x.Attributes.Select(y => new
            {
                key = y.Name,
                value = y.Value
            }))
                .GroupBy(g => g.key)
            .SelectMany(grp => grp.Count() == 1
                ? grp
                : grp.Select((x, i) => new
                {
                    key = x.key + "-" + (i + 1),
                    value = x.value
                }))
            .Select(x => new DocumentContent.MetadataHeader(x.key, x.value))
            .ToList();

            var pages = doc.QuerySelectorAll("div.page");
            var pagesList = new List<DocumentContent.DocumentPage>();
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                var filteredContent = _filterManager.Filter(page.TextContent);
                pagesList.Add(new DocumentContent.DocumentPage(i + 1, filteredContent));
                page.TextContent = filteredContent;
            }

            if (pages.Length == 0)
            {
                meta.Add(new DocumentContent.MetadataHeader(DocumentContent.MetadataWithoutPageInfo, "true"));
                var body = doc.QuerySelector("body");
                var documentContent = body.TextContent;
                if (!String.IsNullOrEmpty(documentContent))
                {
                    var filteredContent = _filterManager.Filter(documentContent);
                    pagesList.Add(new DocumentContent.DocumentPage(1, filteredContent));
                    body.TextContent = filteredContent;
                }
            }

            var content = new DocumentContent(pagesList.ToArray(), meta.ToArray());
            var sb = new StringBuilder();
            using (var tw = new StringWriter(sb))
            {
                doc.ToHtml(tw, HtmlMarkupFormatter.Instance);
            }
            var sanitized = sb.ToString();
            return new ContentFormatBuilderResult()
            {
                Content = content,
                SanitizedTikaContent = sanitized,
            };
        }
    }

    public class ContentFormatBuilderResult
    {
        public DocumentContent Content { get; set; }

        public String SanitizedTikaContent { get; set; }
    }
}
