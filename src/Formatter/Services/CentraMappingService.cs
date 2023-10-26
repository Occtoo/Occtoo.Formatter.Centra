using System.Collections.Generic;
using System.Linq;

namespace outfit_international.Services
{
    /// <summary>
    /// This is where you will do your custom mapping for your customer. 
    /// Data below is a example of a centra setup
    /// </summary>

    public class CentraMappingService : ICentraMappingService
    {
        // Example of mappings
        private readonly List<(int productType, int centraId)> _sizeChartMappings = new()
        {
            (100000,2),
            (100100,2),
            (100200,13),
            (100400,14),
            (110100,15),
            (110102,16),
            (110200,17),
            (110300,18),
            (110400,19),
            (110500,20),
            (110700,21),
            (120000,22),
            (120100,22),
            (120200,23),
            (120300,69),
            (130100,25),
            (130200,26),
            (130400,27),
            (140100,28),
            (140200,29),
            (140300,30),
            (150000,31),
            (150200,32),
            (150300,33),
            (160100,34),
            (160200,35),
            (170000,36),
            (180100,37),
            (180300,37),
            (180301,38),
            (180400,39),
            (180500,40),
            (180600,41),
            (180800,42),
            (190100,43),
            (200100,44),
            (200200,45),
            (200250,46),
            (200300,47),
            (210100,48),
            (210200,49),
            (210300,50),
            (220100,51),
            (220200,52),
            (220300,53),
            (290000,54),
            (290400,55),
            (300000,56),
            (320000,57),
            (330000,58),
            (340000,59),
            (350000,60),
            (360000,61),
            (370000,62),
            (380000,63),
            (540000,64),
            (560000,65),
            (580000,66),
            (650000,67),
            (710000,68),
            (950000,41)
        };
        private readonly List<(int productType, int folderId)> _folderMappings = new()
        {
            (100100,8),
            (100200,15),
            (100400,35),
            (110100,35),
            (110102,35),
            (110200,47),
            (110300,59),
            (110400,35),
            (110500,47),
            (110700,35),
            (120000,68),
            (120100,68),
            (120200,69),
            (120300,59),
            (130100,36),
            (130200,58),
            (130400,94),
            (140100,38),
            (140200,48),
            (140300,63),
            (150000,37),
            (150200,51),
            (150300,64),
            (160100,42),
            (160200,42),
            (170000,39),
            (180100,45),
            (180300,45),
            (180301,45),
            (180400,45),
            (180500,94),
            (180600,94),
            (180800,94),
            (190100,44),
            (200100,41),
            (200200,41),
            (200250,41),
            (200300,41),
            (210100,46),
            (210200,46),
            (210300,46),
            (220100,94),
            (220200,94),
            (220300,94),
            (290000,60),
            (290400,94),
            (300000,65),
            (320000,65),
            (330000,65),
            (340000,65),
            (350000,61),
            (360000,61),
            (370000,61),
            (380000,61),
            (540000,61),
            (560000,61),
            (580000,61),
            (650000,61),
            (710000,61),
            (950000,94)
        };
        private readonly List<(int productType, int measurementId)> _measurementChartMappings = new()
        {
            (100000,19),
            (100100,19),
            (100200,9),
            (110100,28),
            (110102,28),
            (110200,29),
            (110300,30),
            (110400,28),
            (110500,29),
            (110700,28),
            (120000,19),
            (120100,19),
            (120200,9),
            (120300,27),
            (130100,19),
            (130200,9),
            (130400,19),
            (140100,19),
            (140200,9),
            (140300,27),
            (150000,19),
            (150200,9),
            (150300,27),
            (160100,19),
            (160200,19),
            (170000,10),
            (180100,22),
            (180300,22),
            (180301,22),
            (180400,22),
            (180500,22),
            (190100,21),
            (190200,21),
            (200100,19),
            (200200,28),
            (200250,29),
            (200300,31),
            (220200,28),
            (220300,28),
            (290400,31),
            (300000,11),
            (300200,11),
            (320000,11),
            (330000,11)
        };
        private readonly List<(int productType, int canonicalId, List<int> categoryIds)> _categoryMappings = new()
        {
            (100000,16, new List<int>() {16, 10, 145, 153}),
            (100100,16, new List<int>() {16, 10, 145, 153}),
            (100200,60, new List<int>() {60, 59, 146, 155}),
            (100400,19, new List<int>() {19, 10, 145, 153}),
            (110100,19, new List<int>() {19, 10, 145, 153}),
            (110102,19, new List<int>() {19, 10, 145, 153}),
            (110200,61, new List<int>() {61, 59, 146, 155}),
            (110300,176, new List<int>() {176, 157, 147, 76}),
            (110400,19, new List<int>() {19, 10, 145, 153}),
            (110500,61, new List<int>() {61, 59, 146, 155}),
            (110700,19, new List<int>() {19, 10, 145, 153}),
            (120000,111, new List<int>() {111, 10, 145, 153}),
            (120100,111, new List<int>() {111, 10, 145, 153}),
            (120200,67, new List<int>() {67, 59, 146, 155}),
            (120300,102, new List<int>() {102, 157, 147, 76}),
            (130100,27, new List<int>() {27, 10, 145, 153}),
            (130200,64, new List<int>() {64, 59, 146, 155}),
            (130400,16, new List<int>() {16, 10, 145, 153}),
            (140100,29, new List<int>() {29, 10, 145, 153}),
            (140200,65, new List<int>() {65, 59, 146, 155}),
            (140300,78, new List<int>() {78, 157, 147, 76}),
            (150000,24, new List<int>() {24, 10, 145, 153}),
            (150200,63, new List<int>() {63, 59, 146, 155}),
            (150300,79, new List<int>() {79, 157, 147, 76}),
            (160100,35, new List<int>() {35, 10, 145, 153, 59, 146, 155, 72}),
            (160200,35, new List<int>() {35, 10, 145, 153, 59, 146, 155, 72}),
            (170000,167, new List<int>() {167, 10, 145, 153, 59, 146, 155, 162}),
            (180100,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180300,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180301,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180400,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180500,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180600,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (180800,164, new List<int>() {164, 10, 145, 153, 59, 146, 155, 159}),
            (190100,166, new List<int>() {166, 10, 145, 153, 59, 146, 155, 161}),
            (190200,166, new List<int>() {166, 10, 145, 153, 59, 146, 155, 161}),
            (200100,163, new List<int>() {163, 10, 145, 153, 59, 146, 155, 158}),
            (200200,163, new List<int>() {163, 10, 145, 153, 59, 146, 155, 158}),
            (200250,163, new List<int>() {163, 10, 145, 153, 59, 146, 155, 158}),
            (200300,163, new List<int>() {163, 10, 145, 153, 59, 146, 155, 158}),
            (210100,165, new List<int>() {165, 10, 145, 153, 59, 146, 155, 160}),
            (210200,165, new List<int>() {165, 10, 145, 153, 59, 146, 155, 160}),
            (210300,165, new List<int>() {165, 10, 145, 153, 59, 146, 155, 160}),
            (220100,19, new List<int>() {19, 10, 145, 153}),
            (220200,19, new List<int>() {19, 10, 145, 153}),
            (220300,19, new List<int>() {19, 10, 145, 153}),
            (290000,171, new List<int>() {81, 171, 174}),
            (290400,32, new List<int>() {32, 10, 145, 153}),
            (300000,52, new List<int>() {10, 106, 52, 154, 59, 107, 69, 156}),
            (300200,52, new List<int>() {10, 106, 52, 154, 59, 107, 69, 156}),
            (320000,54, new List<int>() {10, 106, 54, 154, 59, 107, 70, 156}),
            (330000,154, new List<int>() {10, 106, 154, 59, 107, 156}),
            (340000,154, new List<int>() {10, 106, 154, 59, 107, 156}),
            (350000,169, new List<int>() {81, 169, 174}),
            (360000,170, new List<int>() {81, 170, 174}),
            (370000,171, new List<int>() {81, 171, 174}),
            (380000,172, new List<int>() {81, 172, 174}),
            (540000,171, new List<int>() {81, 171, 174}),
            (560000,171, new List<int>() {81, 171, 174}),
            (580000,171, new List<int>() {81, 171, 174}),
            (650000,171, new List<int>() {81, 171, 174}),
            (710000,171, new List<int>() {81, 171, 174})
        };
        // end example 

        public int GetCentraSizeChartId(string productType)
        {
            if (!int.TryParse(productType, out int prodType))
                return 0;
            var mappings = _sizeChartMappings.FirstOrDefault(x => x.productType == prodType);
            return mappings.productType == prodType ? mappings.centraId : -1;
        }

        public int GetCentraFolderId(string productType)
        {
            if (!int.TryParse(productType, out int prodType))
                return 0;
            var mappings = _folderMappings.FirstOrDefault(x => x.productType == prodType);
            if (mappings.productType == prodType)
            {
                return mappings.folderId;
            }

            return -1;
        }

        public int GetCentraMeasurementChartId(string productType)
        {
            if (!int.TryParse(productType, out int prodType))
                return 0;
            var mappings = _measurementChartMappings.FirstOrDefault(x => x.productType == prodType);
            if (mappings.productType == prodType)
            {
                return mappings.measurementId;
            }

            return 0;
        }

        public (int, List<int>) GetCentraCanonicalandCategoryIds(string productType)
        {
            if (int.TryParse(productType, out int prodType))
            {
                var mappings = _categoryMappings.FirstOrDefault(x => x.productType == prodType);
                if (mappings.productType == prodType)
                    return (mappings.canonicalId, mappings.categoryIds);
            }
            return (-1, null);
        }
    }
}
