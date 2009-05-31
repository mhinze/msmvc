/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System.Net.Mime;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public abstract class FileResult : ActionResult
	{
		protected FileResult(string contentType)
		{
			if (String.IsNullOrEmpty(contentType))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "contentType");
			}

			ContentType = contentType;
		}

		string _fileDownloadName;

		public string ContentType { get; private set; }

		public string FileDownloadName
		{
			get { return _fileDownloadName ?? String.Empty; }
			set { _fileDownloadName = value; }
		}

		public override void ExecuteResult(ControllerContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			var response = context.HttpContext.Response;
			response.ContentType = ContentType;

			if (!String.IsNullOrEmpty(FileDownloadName))
			{
				// From RFC 2183, Sec. 2.3:
				// The sender may want to suggest a filename to be used if the entity is
				// detached and stored in a separate file. If the receiving MUA writes
				// the entity to a file, the suggested filename should be used as a
				// basis for the actual filename, where possible.
				var disposition = new ContentDisposition {FileName = FileDownloadName};
				var headerValue = disposition.ToString();
				context.HttpContext.Response.AddHeader("Content-Disposition", headerValue);
			}

			WriteFile(response);
		}

		protected abstract void WriteFile(HttpResponseBase response);
	}
}