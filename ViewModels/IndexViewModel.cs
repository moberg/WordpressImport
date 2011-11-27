using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace WordpressImport.ViewModels
{
	public class IndexViewModel {
		public IEnumerable<SelectListItem> Blogs { get; set; }
		public int BlogId;
	}
}
