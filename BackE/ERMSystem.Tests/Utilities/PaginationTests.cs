using System;
using System.Collections.Generic;
using System.Linq;
using ERMSystem.Application.DTOs.Common;
using Xunit;

namespace ERMSystem.Tests.Utilities
{
    public class PaginationTests
    {
        [Fact]
        public void PaginationRequest_DefaultValues()
        {
            var request = new PaginationRequest();

            Assert.Equal(1, request.PageNumber);
            Assert.Equal(10, request.PageSize);
        }

        [Fact]
        public void PaginationRequest_PageSizeExceedsMax_ClampedTo50()
        {
            var request = new PaginationRequest { PageSize = 100 };

            Assert.Equal(50, request.PageSize);
        }

        [Fact]
        public void PaginationRequest_TextSearchAlias_Works()
        {
            var request = new PaginationRequest { TextSeach = "test" };

            Assert.Equal("test", request.TextSearch);
        }

        [Fact]
        public void PaginatedResult_CalculatesTotalPages_Correctly()
        {
            var items = Enumerable.Range(1, 5).Select(i => $"Item{i}");
            var result = new PaginatedResult<string>(items, totalCount: 23, pageNumber: 1, pageSize: 10);

            Assert.Equal(3, result.TotalPages); // ceil(23/10) = 3
            Assert.Equal(23, result.TotalCount);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public void PaginatedResult_ExactDivision_NoExtraPage()
        {
            var items = Enumerable.Empty<int>();
            var result = new PaginatedResult<int>(items, totalCount: 20, pageNumber: 2, pageSize: 10);

            Assert.Equal(2, result.TotalPages); // ceil(20/10) = 2
        }

        [Fact]
        public void PaginatedResult_SingleItem_OnePage()
        {
            var items = new[] { 1 };
            var result = new PaginatedResult<int>(items, totalCount: 1, pageNumber: 1, pageSize: 10);

            Assert.Equal(1, result.TotalPages);
        }

        [Fact]
        public void PaginatedResult_ZeroItems_ZeroPages()
        {
            var items = Enumerable.Empty<string>();
            var result = new PaginatedResult<string>(items, totalCount: 0, pageNumber: 1, pageSize: 10);

            Assert.Equal(0, result.TotalPages);
        }
    }
}
