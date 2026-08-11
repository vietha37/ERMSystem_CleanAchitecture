using System;
using ERMSystem.Application.Utilities;
using Xunit;

namespace ERMSystem.Tests.Utilities
{
    public class CompactCodeGeneratorTests
    {
        [Fact]
        public void Generate_ReturnsCorrectFormat()
        {
            var now = DateTime.UtcNow;
            var code = CompactCodeGenerator.Generate("MR", now);

            Assert.StartsWith("MR-", code);
            Assert.Equal(2 + 1 + 5 + 3, code.Length); // MR- (3) + timePart (5) + randomPart (3) = 11
        }

        [Fact]
        public void Generate_WithCustomRandomLength_ReturnsCorrectLength()
        {
            var now = DateTime.UtcNow;
            var code = CompactCodeGenerator.Generate("LB", now, 5);

            Assert.StartsWith("LB-", code);
            Assert.Equal(2 + 1 + 5 + 5, code.Length); // LB- (3) + timePart (5) + randomPart (5) = 13
        }

        [Fact]
        public void Generate_DifferentPrefixes()
        {
            var now = DateTime.UtcNow;
            var mrCode = CompactCodeGenerator.Generate("MR", now);
            var lbCode = CompactCodeGenerator.Generate("LB", now);

            Assert.StartsWith("MR-", mrCode);
            Assert.StartsWith("LB-", lbCode);
        }

        [Fact]
        public void Generate_NullOrEmptyPrefix_DefaultsToID()
        {
            var now = DateTime.UtcNow;
            var code = CompactCodeGenerator.Generate("", now);

            Assert.StartsWith("ID-", code);
        }
    }
}
