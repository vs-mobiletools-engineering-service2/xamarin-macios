using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Input;
using System.Xml;
using Foundation;
using ObjCRuntime;
using NUnit.Framework;

namespace LinkSdk {

	[TestFixture]
	[Preserve (AllMembers = true)]
	public class PclTest {
		
		[Test]
		public void Corlib ()
		{
			BinaryWriter bw = new BinaryWriter (Stream.Null);
			bw.Dispose ();
		}

		[Test]
		public void System ()
		{
#if __WATCHOS__
			Assert.Ignore ("WatchOS doesn't support BSD sockets, which our network stack currently requires.");
#endif
			const string url = "http://www.google.com";
			Uri uri = new Uri (url);
			
			Assert.False (this is ICommand, "ICommand");
			
			HttpWebRequest hwr = WebRequest.CreateHttp (uri);
			try {
				Assert.True (hwr.SupportsCookieContainer, "SupportsCookieContainer");
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
			
			WebResponse wr = hwr.GetResponse ();
			try {
				Assert.True (wr.SupportsHeaders, "SupportsHeaders");
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
			wr.Dispose ();

			try {
				Assert.NotNull (WebRequest.CreateHttp (url));
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}

			try {
				Assert.NotNull (WebRequest.CreateHttp (uri));
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
		}

		[Test]
		public void ServiceModel ()
		{
			AddressHeaderCollection ahc = new AddressHeaderCollection ();
			try {
				ahc.FindAll ("name", "namespace");
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
			
			try {
				FaultException.CreateFault (new TestFault (), String.Empty, Array.Empty<Type> ());
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
		}

		class TestFault : MessageFault {
			public override FaultCode Code => throw new NotImplementedException ();
			public override bool HasDetail => throw new NotImplementedException ();
			public override FaultReason Reason => throw new NotImplementedException ();
			protected override void OnWriteDetailContents (XmlDictionaryWriter writer)
			{
				throw new NotImplementedException ();
			}
		}

		[Test]
		public void Xml ()
		{
			try {
				XmlConvert.VerifyPublicId (String.Empty);
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}

			try {
				XmlConvert.VerifyWhitespace (String.Empty);
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}

			try {
				XmlConvert.VerifyXmlChars (String.Empty);
			}
			catch (NotImplementedException) {
				// feature is not available, but the symbol itself is needed
			}
			
			var xr = XmlReader.Create (Stream.Null);
			xr.Dispose ();
			
			var xw = XmlWriter.Create (Stream.Null);
			xw.Dispose ();
			
			XmlReaderSettings xrs = new XmlReaderSettings ();
			xrs.DtdProcessing = DtdProcessing.Ignore;
		}
	}
}