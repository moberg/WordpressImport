using Orchard.Blogs;
using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace WordpressImport
{
	public class AdminMenu : INavigationProvider
	{
		public Localizer T { get; set; }
		public string MenuName { get { return "admin"; } }

		public void GetNavigation(NavigationBuilder builder)
		{
			builder.AddImageSet("importexport")
				.Add(T("Wordpress Import"), "42", BuildMenu);
		}

		private void BuildMenu(NavigationItemBuilder menu)
		{
			menu.Add(T("Import"), "0", item => item.Action("Index", "Admin", new { area = "WordpressImport" }).Permission(Permissions.PublishBlogPost).LocalNav());
		}
	}
}
