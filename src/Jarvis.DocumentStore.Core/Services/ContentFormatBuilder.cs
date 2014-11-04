using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Core.Services
{
    public static class ContentFormatBuilder
    {
        public static DocumentContent CreateFromTikaPlain(String tikaFullContent)
        {
            if (String.IsNullOrEmpty(tikaFullContent)) return DocumentContent.NullContent;

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
                pagesList.Add(new DocumentContent.DocumentPage(i, page.TextContent));
            }

            if (pages.Length == 0)
            {
                meta.Add(new DocumentContent.MetadataHeader(DocumentContent.MetadataWithoutPageInfo,"true"));
                pagesList.Add(new DocumentContent.DocumentPage(1, doc.QuerySelector("body").TextContent));
            }

            return new DocumentContent(pagesList.ToArray(), meta.ToArray());
        }
    }
}
