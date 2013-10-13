using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using NUnit.Framework;
using Microsoft.Build.Exceptions;

namespace MonoTests.Microsoft.Build.Construction
{
	[TestFixture]
	public class ProjectRootElementTest
	{
		const string empty_project_xml = "<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003' />";

		[Test]
		[ExpectedException (typeof (UriFormatException))]
		[Category ("NotWorking")] // URL is constructed for ElementLocation, which we don't support yet.
		public void CreateExpectsAbsoluteUri ()
		{
			var xml = XmlReader.Create (new StringReader (empty_project_xml), null, "foo.xml");
			ProjectRootElement.Create (xml);
		}

		[Test]
		public void CreateAndPaths ()
		{
			Assert.IsNull (ProjectRootElement.Create ().FullPath, "#1");
			var xml = XmlReader.Create (new StringReader (empty_project_xml), null, "file:///foo.xml");
			// This creator does not fill FullPath...
			var root = ProjectRootElement.Create (xml);
			Assert.IsNull (root.FullPath, "#2");
			Assert.AreEqual (Path.GetDirectoryName (new Uri (GetType ().Assembly.CodeBase).LocalPath), root.DirectoryPath, "#3");
		}

		[Test]
		public void FullPathSetter ()
		{
			var root = ProjectRootElement.Create ();
			root.FullPath = "test" + Path.DirectorySeparatorChar + "foo.xml";
			var full = Path.Combine (Path.GetDirectoryName (new Uri (GetType ().Assembly.CodeBase).LocalPath), "test", "foo.xml");
			Assert.AreEqual (full, root.FullPath, "#1");
			Assert.AreEqual (Path.GetDirectoryName (full), root.DirectoryPath, "#1");
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void FullPathSetNull ()
		{
			ProjectRootElement.Create ().FullPath = null;
		}

		[Test]
		public void InvalidProject ()
		{
			try {
				ProjectRootElement.Create (XmlReader.Create (new StringReader (" <root/>")));
				Assert.Fail ("should throw InvalidProjectFileException");
			} catch (InvalidProjectFileException ex) {
				#if NET_4_5
				Assert.AreEqual (1, ex.LineNumber, "#1");
				// it is very interesting, but unlike XmlReader.LinePosition it returns the position for '<'.
				Assert.AreEqual (2, ex.ColumnNumber, "#2");
				#endif
			}
		}

		[Test]
		public void CreateWithXmlLoads ()
		{
			string project_xml_1 = "<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'><ItemGroup><None Include='bar.txt' /></ItemGroup></Project>";
			var xml = XmlReader.Create (new StringReader (project_xml_1), null, "file://localhost/foo.xml");
			var root = ProjectRootElement.Create (xml);
			Assert.AreEqual (1, root.Items.Count, "#1");
		}

		[Test]
		public void LoadUnknownChild ()
		{
			string project_xml_1 = "<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'><Unknown /></Project>";
			var xml = XmlReader.Create (new StringReader (project_xml_1), null, "file://localhost/foo.xml");
			try {
				ProjectRootElement.Create (xml);
				Assert.Fail ("should throw InvalidProjectFileException");
			} catch (InvalidProjectFileException ex) {
				#if NET_4_5
				Assert.AreEqual (1, ex.LineNumber, "#1");
				// unlike unexpected element case which returned the position for '<', it does return the name start char...
				Assert.AreEqual (70, ex.ColumnNumber, "#2");
				#endif
			}
		}

		[Test]
		public void LoadUnregisteredItem ()
		{
			string project_xml_1 = "<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'><ItemGroup><UnregisteredItem Include='bar.txt' /></ItemGroup></Project>";
			var xml = XmlReader.Create (new StringReader (project_xml_1), null, "file://localhost/foo.xml");
			var root = ProjectRootElement.Create (xml);
			Assert.AreEqual (1, root.Items.Count, "#1");
		}
		
		[Test]
		public void LoadInvalidProjectForBadCondition ()
		{
			string xml = @"<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <Foo>What are 'ESCAPE' &amp; ""EVALUATE"" ? $ # % ^</Foo>
    <!-- Note that this contains invalid Condition expression. Project.ctor() fails to load. -->
    <Baz Condition=""$(Void)=="">$(FOO)</Baz>
  </PropertyGroup>
</Project>";
			var path = "file://localhost/foo.xml";
			var reader = XmlReader.Create (new StringReader (xml), null, path);
			var root = ProjectRootElement.Create (reader);
			Assert.AreEqual (2, root.Properties.Count, "#1");
		}
	}
}
