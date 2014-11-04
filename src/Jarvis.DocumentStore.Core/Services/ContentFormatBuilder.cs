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
        public static DocumentRevisionContent CreateFromTikaPlain(String tikaFullContent)
        {
            if (String.IsNullOrEmpty(tikaFullContent)) return DocumentRevisionContent.NullContent;

            var doc = DocumentBuilder.Html(tikaFullContent);

            var allMeta = doc.QuerySelectorAll("meta");

            var meta = allMeta.SelectMany(x => x.Attributes.Select(y => new{
                key = y.Name,
                value = y.Value
            }))
            .GroupBy(g => g.key)
            .SelectMany(grp => grp.Count() == 1 ? grp : grp.Select((x, i) => new
            {
                key = x.key + "-" + (i + 1),
                value = x.value
            }))
            .Select(x => new DocumentRevisionContent.MetadataHeader(x.key, x.value))
            .ToArray();

            var pages = doc.QuerySelectorAll("div.page");
            var pagesList = new List<DocumentRevisionContent.DocumentPage>();
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                pagesList.Add(new DocumentRevisionContent.DocumentPage(i, page.TextContent));
            }

            return new DocumentRevisionContent(pagesList.ToArray(), meta);
        }
    }
}
