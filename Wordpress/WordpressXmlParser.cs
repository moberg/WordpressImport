using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WordpressImport.Models;

namespace WordpressImport.Wordpress
{
	public class WordpressXmlParser 
	{
		private static XNamespace blogMLNs = "http://www.blogml.com/2006/09/BlogML";
		private static XNamespace dc = "http://purl.org/dc/elements/1.1/";
		private static XNamespace wp = "http://wordpress.org/export/1.1/";
		private static XNamespace excerptNs = "http://wordpress.org/export/1.1/excerpt/";
		private static XNamespace contentNs = "http://purl.org/rss/1.0/modules/content/";

		private static CultureInfo ci = new CultureInfo("en-US");
		private static string dateFormat = "yyyy-MM-ddTHH:mm:ss";

		private XElement channel;

		public WordpressXmlParser(Stream xml) 
		{
			channel = XElement.Load(xml).Element("channel");
		}

		public IEnumerable<WpPost> GetPosts()
		{
			return (from post in channel.Elements("item")
					where post.Element(wp + "post_type").Value == "post"
					select new WpPost
					{
						Title = post.Element("title").Value,
						PostName = post.Element(wp + "post_name").Value,
						Date = ParseWPDate(post.Element("pubDate").Value),
						AuthorName = ((XText)post.Element(dc + "creator").FirstNode).Value,
						CategorieNames = from c in post.Elements("category")
										 where c != null && c.Attribute("domain") != null && c.Attribute("domain").Value == "category"
										 select ((XText)c.FirstNode).Value,
						Content = post.Element(contentNs + "encoded") != null ? post.Element(contentNs + "encoded").Value : null,
						Exerpt = post.Element(excerptNs + "encoded").Value,
						Id = post.Element(wp + "post_id").Value,
						Link = post.Element("link").Value,
						Description = post.Element("description").Value,
						Comments = (from comment in post.Elements(wp + "comment")
									where comment.Element(wp + "comment_approved").Value == "1"
									select new WpComment
									{
										Id = comment.Element(wp + "comment_id").Value,
										AuthorName = comment.Element(wp + "comment_author").Value,
										AuthorEmail = comment.Element(wp + "comment_author_email").Value,
										AuthorIp = comment.Element(wp + "comment_author_IP").Value,
										AuthorUrl = comment.Element(wp + "comment_author_url").Value,
										Date = ParseWPDate(comment.Element(wp + "comment_date_gmt").Value),
										Content = ((XCData)comment.Element(wp + "comment_content").FirstNode).Value,
										Approved = comment.Element(wp + "comment_approved").Value == "1"
									})
					});
		}

		public IEnumerable<WpCategory> GetCategories()
		{
			return 
				(from cat in channel.Elements(wp + "category").Elements(wp + "cat_name")
				select ((XCData)cat.FirstNode).Value)
				.Select((name, i) => new WpCategory { Name = name, Id = i + 1000 });
		}

		public IEnumerable<WpAuthor> GetAuthors()
		{
			return (from creator in channel.Elements("item").Elements(dc + "creator")
						   select ((XText)creator.FirstNode).Value).Distinct()
						.Select((name, i) => new WpAuthor { Name = name, Id = i + 1000 });
		}

		public IEnumerable<string> GetTags()
		{
			return (from tag in channel.Elements(wp + "tag").Elements(wp + "tag_name")
						select ((XCData)tag.FirstNode).Value);
		}

		static DateTime ParseWPDate(string value) {
			return DateTime.Parse(value);
			//return DateTime.ParseExact(value, "ddd, dd MMM yyyy HH:mm:ss zz00", ci.DateTimeFormat);
		}

		static DateTime ParseWPPostDate(string value)
		{
			return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", ci.DateTimeFormat, DateTimeStyles.AssumeUniversal);
		}
	}
}
