using Microsoft.Pex.Framework.Generated;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEViz.Demos;
// <auto-generated>
// This file contains automatically generated tests.
// Do not modify this file manually.
// 
// If the contents of this file becomes outdated, you can delete it.
// For example, if it no longer compiles.
// </auto-generated>
using System;

namespace SEViz.Demos.Tests
{
    public partial class SimpleTest
    {

[TestMethod]
[PexGeneratedBy(typeof(SimpleTest))]
public void SwitchBranchingTest535()
{
    int i;
    Simple s0 = new Simple();
    i = this.SwitchBranchingTest(s0, 0);
    Assert.AreEqual<int>(0, i);
    Assert.IsNotNull((object)s0);
}

[TestMethod]
[PexGeneratedBy(typeof(SimpleTest))]
public void SwitchBranchingTest631()
{
    int i;
    Simple s0 = new Simple();
    i = this.SwitchBranchingTest(s0, 1);
    Assert.AreEqual<int>(-1, i);
    Assert.IsNotNull((object)s0);
}

[TestMethod]
[PexGeneratedBy(typeof(SimpleTest))]
public void SwitchBranchingTest707()
{
    int i;
    Simple s0 = new Simple();
    i = this.SwitchBranchingTest(s0, 2);
    Assert.AreEqual<int>(-2, i);
    Assert.IsNotNull((object)s0);
}

[TestMethod]
[PexGeneratedBy(typeof(SimpleTest))]
[PexRaisedException(typeof(DivideByZeroException))]
public void SwitchBranchingTestThrowsDivideByZeroException852()
{
    int i;
    Simple s0 = new Simple();
    i = this.SwitchBranchingTest(s0, 3);
}
    }
}
