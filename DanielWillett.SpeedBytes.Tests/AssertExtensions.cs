namespace DanielWillett.SpeedBytes.Tests;
public static class AssertExtensions
{
    public static void SequenceIsEqual<T>(this Assert assert, IList<T> expected, IList<T> actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; ++i)
            Assert.AreEqual(expected[i], actual[i]);
    }
}
