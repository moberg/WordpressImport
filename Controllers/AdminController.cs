using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using Orchard;
using Orchard.Blogs;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.Caching;
using Orchard.Comments.Models;
using Orchard.Comments.Services;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.Models;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Tags.Models;
using Orchard.Tags.Services;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using WordpressImport.Models;
using WordpressImport.ViewModels;
using WordpressImport.Wordpress;

namespace WordpressImport.Controllers
{
	[Admin]
	public class AdminController : Controller
	{
		private readonly ISignals _signals;
		private readonly IBlogService _blogService;
		private readonly IBlogPostService _blogPostService;
		private readonly ITagService _tagsService;
		private readonly ICommentService _commentService;

		public AdminController(
			IOrchardServices services,
			ISignals signals,
			IBlogService blogService, 
			IBlogPostService blogPostService, 
			ITagService tagsService,
			ICommentService commentService,
			IContentManager contentManager)
		{
			_signals = signals;
			_blogService = blogService;
			_blogPostService = blogPostService;
			_tagsService = tagsService;
			_commentService = commentService;
			Services = services;
		}

		public IOrchardServices Services { get; set; }
		public Localizer T { get; set; }

		public ActionResult Index()
		{
			if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not allowed to manage cache")))
				return new HttpUnauthorizedResult();

			var viewModel = new IndexViewModel
			{
				Blogs = _blogService.Get()
										.Select(x => new SelectListItem
										{
											Text = x.Name,
											Value = x.Id.ToString()
										}).ToList()
			};

			return View(viewModel);
		}

		[HttpPost, ActionName("Index")]
		public ActionResult IndexPost(IndexViewModel viewModel)
		{
			if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not allowed to manage cache")))
				return new HttpUnauthorizedResult();

			var wordpress = new WordpressXmlParser(Request.Files[0].InputStream);
			var blog = _blogService.Get(int.Parse(Request.Params["BlogId"]), VersionOptions.Latest).As<BlogPart>();

            if (blog == null)
                return HttpNotFound();

			var posts = wordpress.GetPosts().ToList();
			foreach (var post in posts) {

				var uri = new Uri(post.Link);
				var slug = uri.PathAndQuery.TrimStart('/');
				var blogPost = Services.ContentManager.New<BlogPostPart>("BlogPost");

				blogPost.BlogPart = blog;
				blogPost.Title = post.Title;
				blogPost.Text = post.Content;
				blogPost.Get<CommonPart>().CreatedUtc = post.Date;

				var isPublished = !slug.StartsWith("?");

				if (isPublished) {
					blogPost.Slug = slug;
				}

				if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't create blog post"))) {
					return new HttpUnauthorizedResult();
				}

				Services.ContentManager.Create(blogPost, isPublished ? VersionOptions.Published : VersionOptions.Draft);

				_tagsService.UpdateTagsForContentItem(blogPost.ContentItem, post.CategorieNames);

				foreach (var wpComment in post.Comments) {
					WpComment wpComment1 = wpComment;
					var fun = Services.ContentManager.Create<CommentPart>("Comment", comment => {
					    comment.Record.Author = wpComment1.AuthorName;
					    comment.Record.CommentDateUtc = wpComment1.Date;
					    comment.Record.CommentText = wpComment1.Content;
					    comment.Record.Email = wpComment1.AuthorEmail;
					    comment.Record.SiteName = wpComment1.AuthorUrl;
						comment.Record.UserName = null;
					    comment.Record.CommentedOn = blogPost.Id;
						comment.Record.Status = CommentStatus.Approved;
					    
						var commentedOn = Services.ContentManager.Get<ICommonPart>(comment.Record.CommentedOn);
					    if (commentedOn != null && commentedOn.Container != null) {
					        comment.Record.CommentedOnContainer = commentedOn.Container.ContentItem.Id;
					    }
					});
				}
			}

			Services.Notifier.Information(T("Imported {0} blog posts", posts.Count));	
			return RedirectToAction("Index");
		}
	}
}
