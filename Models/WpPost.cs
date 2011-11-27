using System;
using System.Collections.Generic;

namespace WordpressImport.Models {
	public class WpPost
	{
		public string Title;
		public string PostName;
		public DateTime Date;
		public string AuthorName;
		public IEnumerable<string> CategorieNames;
		public string Content;
		public string Exerpt;
		public string Id;
		public string Link;
		public string Description;
		public IEnumerable<WpComment> Comments;
	}
}