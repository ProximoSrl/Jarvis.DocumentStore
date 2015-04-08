using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Jobs.Tika.Filters;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public class ContentFormatBuilder
    {

        private ContentFilterManager _filterManager;

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

            var doc = DocumentBuilder.Html(tikaFullContent);

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
            var sanitized = doc.ToHtml();
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
