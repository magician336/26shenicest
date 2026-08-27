using DoNotForgetMe.Network;
using NUnit.Framework;

public class RoomCodeGeneratorTests
{
    [Test]
    public void NormalizeRemovesWhitespaceAndUppercasesCode()
    {
        Assert.AreEqual("ABCD", RoomCodeGenerator.Normalize(" a b\tc\nd "));
    }

    [Test]
    public void NormalizedSpacedCodeCanBeValidated()
    {
        var normalizedCode = RoomCodeGenerator.Normalize(" a b c d ");

        Assert.IsTrue(RoomCodeGenerator.IsValid(normalizedCode));
    }

    [Test]
    public void IsValidRejectsAmbiguousCharacters()
    {
        Assert.IsFalse(RoomCodeGenerator.IsValid("0O1IL"));
    }
}
