using System;

namespace WordpressImport.Models {
	public class WpComment {
		public string Id;
		public string AuthorName;
		public string AuthorEmail;
		public string AuthorIp;
		public string AuthorUrl;
		public DateTime Date;
		public string Content;
		public bool Approved;
	}
}