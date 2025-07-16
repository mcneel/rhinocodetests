using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Storage;

namespace Rhino.Runtime.Code.Tests
{
  [TestFixture]
  public class StorageExtensionTests
  {
    [Test]
    public void TestStorage_Uri_Title()
    {
      // this used to be test_rhinocode_uri_title.cs
      var u = new Uri("C:/dsfsdf/sdfds/fds%$^%#^#$45656456.fs");

      var title = u.GetEndpointTitle();
      Assert.That(title == "fds%$^%#^#$45656456.fs");

      var ext = u.GetEndpointExt();
      Assert.That(ext == ".fs");

      var noext = u.GetEndpointTitleNoExt();
      Assert.That(noext == "fds%$^%#^#$45656456");
    }
  }
}