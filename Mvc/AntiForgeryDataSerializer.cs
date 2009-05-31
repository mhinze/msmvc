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

using System.IO;
using System.Web.Mvc.Resources;
using System.Web.UI;

namespace System.Web.Mvc
{
	internal class AntiForgeryDataSerializer
	{
		IStateFormatter _formatter;

		protected internal IStateFormatter Formatter
		{
			get
			{
				if (_formatter == null)
				{
					_formatter = FormatterGenerator.GetFormatter();
				}
				return _formatter;
			}
			set { _formatter = value; }
		}

		static HttpAntiForgeryException CreateValidationException(Exception innerException)
		{
			return new HttpAntiForgeryException(MvcResources.AntiForgeryToken_ValidationFailed, innerException);
		}

		public virtual AntiForgeryData Deserialize(string serializedToken)
		{
			if (String.IsNullOrEmpty(serializedToken))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "serializedToken");
			}

			// call property getter outside try { } block so that exceptions bubble up for debugging
			var formatter = Formatter;

			try
			{
				var deserializedObj = (Triplet)formatter.Deserialize(serializedToken);
				return new AntiForgeryData
				{
					Salt = (string)deserializedObj.First,
					Value = (string)deserializedObj.Second,
					CreationDate = (DateTime)deserializedObj.Third
				};
			}
			catch (Exception ex)
			{
				throw CreateValidationException(ex);
			}
		}

		public virtual string Serialize(AntiForgeryData token)
		{
			if (token == null)
			{
				throw new ArgumentNullException("token");
			}

			var objToSerialize = new Triplet
			{
				First = token.Salt,
				Second = token.Value,
				Third = token.CreationDate
			};

			var serializedValue = Formatter.Serialize(objToSerialize);
			return serializedValue;
		}

		// See http://www.yoda.arachsys.com/csharp/singleton.html (fifth version - fully lazy) for the singleton pattern
		// used here. We need to defer the call to TokenPersister.CreateFormatterGenerator() until we're actually
		// servicing a request, else HttpContext.Current might be invalid in TokenPersister.CreateFormatterGenerator().
		static class FormatterGenerator
		{
			public static readonly Func<IStateFormatter> GetFormatter = TokenPersister.CreateFormatterGenerator();

			// This type is very difficult to unit-test because Page.ProcessRequest() requires mocking
			// much of the hosting environment. For now, we can perform functional tests of this feature.
			sealed class TokenPersister : PageStatePersister
			{
				TokenPersister(Page page)
					: base(page) {}

				public static Func<IStateFormatter> CreateFormatterGenerator()
				{
					// This code instantiates a page and tricks it into thinking that it's servicing
					// a postback scenario with encrypted ViewState, which is required to make the
					// StateFormatter properly decrypt data. Specifically, this code sets the
					// internal Page.ContainsEncryptedViewState flag.
					var writer = TextWriter.Null;
					var response = new HttpResponse(writer);
					var request = new HttpRequest("DummyFile.aspx", HttpContext.Current.Request.Url.ToString(),
					                              "__EVENTTARGET=true&__VIEWSTATEENCRYPTED=true");
					var context = new HttpContext(request, response);

					var page = new Page
					{
						EnableViewStateMac = true,
						ViewStateEncryptionMode = ViewStateEncryptionMode.Always
					};
					page.ProcessRequest(context);

					return () => new TokenPersister(page).StateFormatter;
				}

				public override void Load()
				{
					throw new NotImplementedException();
				}

				public override void Save()
				{
					throw new NotImplementedException();
				}
			}
		}
	}
}