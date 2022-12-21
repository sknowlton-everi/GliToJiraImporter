using GliToJiraImporter.Models;

namespace GliToJiraImporter.Testing.Extensions
{
    public static class ComparisonsExtensions
    {
        public static void TestAssertCategoryModels(this IList<CategoryModel> expectedResult, IList<CategoryModel> result)
        {
            Assert.That(result, !Is.Null);
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count), "The result count of categories does not match the expected.");
            for (int i = 0; i < result.Count; i++)
            {
                expectedResult[i].TestAssertCategoryModel(result[i]);
            }
        }

        public static void TestAssertCategoryModel(this CategoryModel expectedResult, CategoryModel result)
        {
            Assert.That(result, !Is.Null);
            Assert.That(result.Category, Is.EqualTo(expectedResult.Category), $"The Category of {result.Category} does not match the expected.");
            Assert.That(result.RegulationList.Count, Is.EqualTo(expectedResult.RegulationList.Count), $"The RegulationList count of \"{result.Category}\" does not match the expected.");

            for (int i = 0; i < result.RegulationList.Count; i++)
            {
                expectedResult.RegulationList[i].TestAssertRegulationModel(result.RegulationList[i]);
            }
        }

        public static void TestAssertRegulationModel(this RegulationModel expectedResult, RegulationModel result)
        {
            Assert.That(result, !Is.Null);
            Assert.That(result.ClauseId.BaseClauseId, Is.EqualTo(expectedResult.ClauseId.BaseClauseId), $"The BaseClauseId of {result.ClauseId.BaseClauseId} does not match the expected.");
            Assert.That(result.ClauseId.FullClauseId, Is.EqualTo(expectedResult.ClauseId.FullClauseId), $"The FullClauseId of {result.ClauseId.FullClauseId} does not match the expected.");
            Assert.That(result.Subcategory, Is.EqualTo(expectedResult.Subcategory), $"The Subcategory of {result.ClauseId.FullClauseId} does not match the expected.");
            Assert.That(result.Description.Text, Is.EqualTo(expectedResult.Description.Text), $"The Description Text of {result.ClauseId.FullClauseId} does not match the expected.");

            IList<PictureModel> resultAttachmentList = result.Description.AttachmentList;
            IList<PictureModel> expectedResultAttachmentList = expectedResult.Description.AttachmentList;
            Assert.That(resultAttachmentList.Count, Is.EqualTo(expectedResultAttachmentList.Count), $"The AttachmentList of {result.ClauseId.FullClauseId} does not match the expected.");
            for (int i = 0; i < resultAttachmentList.Count; i++)
            {
                Assert.That(resultAttachmentList[i].ImageName, Is.EqualTo(expectedResultAttachmentList[i].ImageName), $"ImageName at position {i} of {result.ClauseId.FullClauseId} does not match the expected.");
                Assert.That(resultAttachmentList[i].ImageBytes, Is.EqualTo(expectedResultAttachmentList[i].ImageBytes), $"ImageBytes at position {i} of {result.ClauseId.FullClauseId} does not match the expected.");
            }
        }
    }
}
