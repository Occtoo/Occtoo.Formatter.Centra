using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using outfit_international.Dtos;
using outfit_international.Dtos.Centra;
using outfit_international.Dtos.Queries;
using outfit_international.Exceptions;
using outfit_international.Helpers;
using outfit_international.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public class CentraService : ICentraService
    {
        #region Environments
        private readonly string _url = Settings.EnvironmentUrl;
        private readonly string _centraAuthToken = Settings.EnvironmentToken;
        #endregion

        #region Variables and constants
        private const int LIMIT = 200;
        private const int MAX_TRYOUTS = 3;
        private const string _objectType = "ProductVariant";
        private const string _attrElementTextKey = "text";

        private static List<StoreDto> _stores;
        private static List<MarketDto> _markets;
        private static IOcctooProductService _occtooProductSvcImportProd;
        #endregion

        #region ImportProducts
        private IOcctooProductService CreateOcctoProductService() =>
            new OcctooProductService(250);

        private bool IsMarketExcluded(MarketDto marketDto, string brandSufix, List<string> excludedFromMarkets)
        {
            foreach (var market in excludedFromMarkets)
            {
                var marketWithBrand = $"{market.ToLower()} {brandSufix.ToLower()}";
                if (marketDto.name.ToLower() == marketWithBrand)
                    return true;
            }
            return false;
        }

        private async Task ImportProductHierarchyAsync(ProductHierarchyDto productHierarchy, List<StoreDto> stores, List<MarketDto> markets)
        {
            if (productHierarchy == null)
                throw new ArgumentException($"Argument {nameof(productHierarchy)} is null.");

            var productId = productHierarchy.CentraId;
            var exceptions = new List<string>() { };

            var weightStr = productHierarchy.Weight ?? string.Empty;
            if (weightStr.Contains(','))
                weightStr = weightStr.Replace(',', '.');
            var weight = float.TryParse(weightStr, out var w) ? w : 0;

            if (productId == 0)
            {
                var result = await ExecGraphQLMutationAsync<OnlyIdDto>("createProduct", $"input: {{ {(productHierarchy.BrandId != 0 ? $"brand: {{ id: {productHierarchy.BrandId} }}," : "")} {(productHierarchy.CollectionId != 0 ? $"collection: {{ id: {productHierarchy.CollectionId} }}," : "")} assignDynamicAttributes: [], assignMappedAttributes: [], {(productHierarchy.FolderId != 0 ? $"folder: {{ id: {productHierarchy.FolderId} }}," : "")} {(productHierarchy.MeasurementChartId != 0 ? $"measurementTable: {{ inherited: true measurementChart: {{ id: {productHierarchy.MeasurementChartId} }} }} " : "")} name: \"{productHierarchy.Name}\", productNumber: \"{productHierarchy.ProductNumber}\", status: {productHierarchy.Status}, weight: {{ unit: KILOGRAMS, value: {weight} }}, internalComment: \"{productHierarchy.InternalComment}\" }}", "product", "id");
                if (result == null)
                    throw new Exception($"ERR:createProduct-{productHierarchy.ProductNumber}");
                productId = result.id;
            }
            else
                await ExecGraphQLMutationAsync<OnlyIdDto>("updateProduct", $"id: {productId}, input: {{ {(productHierarchy.BrandId != 0 ? $"brand: {{ id: {productHierarchy.BrandId} }}," : "")} {(productHierarchy.CollectionId != 0 ? $"collection: {{ id: {productHierarchy.CollectionId} }}," : "")} assignDynamicAttributes: [], assignMappedAttributes: [], {(productHierarchy.FolderId != 0 ? $"folder: {{ id: {productHierarchy.FolderId} }}," : "")} {(productHierarchy.MeasurementChartId != 0 ? $"measurementTable: {{ inherited: true measurementChart: {{ id: {productHierarchy.MeasurementChartId} }} }} " : "")} name: \"{productHierarchy.Name}\", productNumber: \"{productHierarchy.ProductNumber}\", status: {productHierarchy.Status}, weight: {{ unit: KILOGRAMS, value: {weight} }}, internalComment: \"{productHierarchy.InternalComment}\" }}", "product", "id");

            foreach (var variant in productHierarchy.Variants)
            {
                var variantId = variant.CentraId;
                var variantExisted = variantId != 0;
                try
                {
                    if (variantId == 0)
                    {
                        var result = await ExecGraphQLMutationAsync<OnlyIdDto>("createProductVariant", $"input: {{ product: {{ id: {productId} }}, sizeChart: {{ id: {variant.CentraSizeChartId} }}, name: \"{variant.Name}\", variantNumber: \"{variant.VariantNumber}\", internalName: \"{variant.InternalName}\", assignMappedAttributes: [], assignDynamicAttributes: [], status: {variant.Status} }}", "productVariant", "id");
                        if (result == null)
                            throw new Exception($"ERR:createProductVariant-{productHierarchy.ProductNumber}:{productId}");
                        variantId = result.id;
                        variant.CentraId = result.id;
                    }
                    else
                        await ExecGraphQLMutationAsync<OnlyIdDto>("updateProductVariant", $"id: {variantId}, input: {{ name: \"{variant.Name}\", variantNumber: \"{variant.VariantNumber}\", internalName: \"{variant.InternalName}\", assignMappedAttributes: [], assignDynamicAttributes: [], status: {variant.Status} }}", "productVariant", "id");
                }
                catch (CentraServerError)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex.Message);
                    variantId = 0;
                }

                if (variantId <= 0)
                    continue;

                var mappedAttributes = await GetMappedAttributesAsync("productVariants", variantId);
                foreach (var mappedAttribute in mappedAttributes)
                    foreach (var val in mappedAttribute.elements)
                    {
                        var index = variant.AttributeTypeName.IndexOf(mappedAttribute.type.name);
                        if (index > -1)
                        {
                            var mappedAttributeId = await GetMappedAttributeIdAsync(val.value, variant.AttributeTypeName[index], _objectType);
                            if (mappedAttributeId > 0)
                                await UnassignMappedAttributeAsync(variantId, _objectType, variant.AttributeTypeName[index], mappedAttributeId);

                            if (val.value.IndexOf("|") > -1)
                            {
                                var vals = val.value.Split("|");
                                foreach (var singleVal in vals)
                                {
                                    mappedAttributeId = await GetMappedAttributeIdAsync(singleVal, variant.AttributeTypeName[index], _objectType);
                                    if (mappedAttributeId > 0)
                                        await UnassignMappedAttributeAsync(variantId, _objectType, variant.AttributeTypeName[index], mappedAttributeId);
                                }
                            }
                        }
                    }

                foreach (var size in variant.ProductSizes)
                {
                    try
                    {
                        if (size.CentraId == 0)
                            await ExecGraphQLMutationAsync<OnlyIdDto>("createProductSize", $"input: {{ productVariant: {{ id: {variantId} }}, size: {{ {(size.CentraSizeId > 0 ? $"id: {size.CentraSizeId}," : "")} }}, sizeNumber: \"{size.SizeNumber}\", ean: \"{size.EAN}\" }}", "productSize", "id");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add($"[{size.Name}-{size.SizeNumber}]:{ex.Message}");
                    }
                }

                for (var i = 0; i < variant.AttributeTypeName.Count; i++)
                    if (!string.IsNullOrEmpty(variant.AttributeTypeValue[i]))
                    {
                        var allAttrIds = new List<int>() { };

                        if (variant.AttributeTypeValue[i].IndexOf("|") > -1)
                        {
                            var vals = variant.AttributeTypeValue[i].Split("|");
                            foreach (var val in vals)
                            {
                                var mappedAttrId = await GetMappedAttributeIdAsync(val, variant.AttributeTypeName[i], _objectType);
                                if (mappedAttrId == 0)
                                    mappedAttrId = await CreateMappedAttributeAsync(val, variant.AttributeTypeName[i], _attrElementTextKey, val);

                                allAttrIds.Add(mappedAttrId);
                            }
                        }
                        else
                        {
                            var mappedAttrId = await GetMappedAttributeIdAsync(variant.AttributeTypeValue[i], variant.AttributeTypeName[i], _objectType);
                            if (mappedAttrId == 0)
                            {
                                if (variant.AttributeTypeName[i] == "filter_color")
                                    allAttrIds.Add(await CreateColorNameMappedAttributeAsync(variant.AttributeTypeValue[i], variant.AttributeTypeName[i], variant.AttributeTypeValue[i]));
                                else
                                    allAttrIds.Add(await CreateMappedAttributeAsync(variant.AttributeTypeValue[i], variant.AttributeTypeName[i], _attrElementTextKey, variant.AttributeTypeValue[i]));
                            }
                            else
                                allAttrIds.Add(mappedAttrId);
                        }

                        foreach (var attrId in allAttrIds)
                            await AssignMappedAttributeAsync(variantId, _objectType, variant.AttributeTypeName[i], attrId);
                    }

                if (variantExisted)
                {
                    var existingMedias = await GetMediaDtosForProductVariantAsync(variantId);
                    foreach (var existingMedia in existingMedias.media)
                        await ExecGraphQLMutationAsync<object>("deleteProductMedia", $"id: {existingMedia.id}");
                }

                var mediaInput = "";
                foreach (var media in variant.MediaUrls)
                {
                    var singleMedia = $"{{ mediaType: IMAGE, productId: {productId}, variantId: {variantId}, url: \"{media}\" }}";
                    mediaInput = mediaInput == "" ? singleMedia : $"{mediaInput}, {singleMedia}";
                }
                if (mediaInput != "")
                    await ExecGraphQLMutationAsync<object>("createMediaBatch", $"input: {{ productMedia: [{mediaInput}] }}");
            }

            var storeId = 0;
            var brandSufix = "";
            var newMarkets = new List<int>() { };
            var marketsStr = "";
            foreach (var market in markets)
                if (!IsMarketExcluded(market, brandSufix, productHierarchy.ExcludedFromMarkets))
                {
                    newMarkets.Add(market.id);
                    marketsStr += marketsStr != "" ? $", {{ id: {market.id} }}" : $"{{ id: {market.id} }}";
                }

            var selVariant = (ProductVariantDto)null;
            var selVarSeasonCode = 0;
            foreach (var productVariant in productHierarchy.Variants)
            {
                if (productVariant.CentraId == 0)
                    continue;

                if (!int.TryParse(productVariant.SeasonCode, out int seasonCode))
                    seasonCode = 0;

                if (selVariant == null || seasonCode > selVarSeasonCode)
                {
                    selVariant = productVariant;
                    selVarSeasonCode = seasonCode;
                }
            }

            if (selVariant != null)
            {
                var displayId = await GetDisplayIdAsync(productId);

                var oldCategories = await GetDisplayCategoriesAsync(displayId);
                var oldMarkets = await GetDisplayMarketsAsync(displayId);

                var mediaStr = "";
                var productVariantStr = $"{{ productVariant: {{ id: {selVariant.CentraId} }} }} ";
                var uriTmp = "";
                var displayName = "";
                foreach (var variant in productHierarchy.Variants)
                {
                    if (variant.CentraId == 0)
                        continue;

                    var variantMediaIds = await GetMediaDtosForProductVariantAsync(variant.CentraId);
                    if (variant.MediaUrls?.Count > 0)
                    {
                        var repeat = 0;
                        while (variantMediaIds.media.Length != variant.MediaUrls.Count)
                        {
                            Thread.Sleep(500);
                            variantMediaIds = await GetMediaDtosForProductVariantAsync(variant.CentraId);
                            repeat++;
                            if (repeat > 20)
                                break;
                        }
                        if (variantMediaIds.media.Length != variant.MediaUrls.Count)
                            exceptions.Add($"Display media files cannot be loaded for variant: [{variant.VariantNumber}]");

                        foreach (var media in variantMediaIds.media)
                            mediaStr += (mediaStr != "") ? $", {{ productMedia: {{ id: {media.id} }} }}" : $"{{ productMedia: {{ id: {media.id} }} }}";
                    }

                    if (variant != selVariant)
                        productVariantStr += $"{{ productVariant: {{ id: {variant.CentraId} }} }} ";
                }

                displayName = $"{productHierarchy.Name}";
                uriTmp = productHierarchy.Name.Trim().Replace(' ', '-').ToLower() + $"-{productHierarchy.ProductNumber}";

                var utf8bytes = Encoding.UTF8.GetBytes(uriTmp);
                uriTmp = Encoding.ASCII.GetString(Encoding.Convert(Encoding.UTF8, Encoding.ASCII, utf8bytes));
                uriTmp = uriTmp.Replace('?', 'a');

                var uri = "";
                for (var i = 0; i < uriTmp.Length; i++)
                    if (char.IsLetterOrDigit(uriTmp[i]) || uriTmp[i] == '-' || uriTmp[i] == '_')
                        uri += uriTmp[i];
                    else
                        uri += "-";

                var canonicalCategId = selVariant.CentraCanonicalCategoryId;
                var categIdStr = "";
                foreach (var cetegoryId in selVariant.CentraCategoryIds)
                    categIdStr += $"{{ id: {cetegoryId} }} ";

                var desc = productHierarchy.Description;
                if (desc != null)
                {
                    while (desc.IndexOf('\n') > -1)
                        desc = desc.Remove(desc.IndexOf('\n'), 1);
                    while (desc.IndexOf('\"') > -1)
                        desc = desc.Remove(desc.IndexOf('\"'), 1);
                }

                if (displayId == 0)
                    await ExecGraphQLMutationAsync<object>("createDisplay", $"input: {{ store: {{ id: {storeId} }} addMarkets: [{marketsStr}] product: {{ id: {productId} }} name: \"{displayName}\" description: \"{productHierarchy.Description}\" status: {selVariant.Status} tags: [] addProductVariants: [{productVariantStr}] addCategories: [ {categIdStr} ] canonicalCategory: {{ id: {canonicalCategId} }} uri: \"{uri}\" addProductMedia: [{mediaStr}] }}");
                else
                {
                    var removeCategories = "";
                    if (oldCategories.Count > 0)
                    {
                        removeCategories = "removeCategories: [";
                        foreach (var oldCategory in oldCategories)
                            if (!selVariant.CentraCategoryIds.Contains(oldCategory))
                                removeCategories += $"{{ id: {oldCategory} }}";
                        removeCategories += " ]";
                    }

                    var removeMarkets = "";
                    if (oldMarkets.Count > 0)
                    {
                        removeMarkets = "removeMarkets: [";
                        foreach (var oldMarket in oldMarkets)
                            if (!newMarkets.Contains(oldMarket))
                                removeMarkets += $"{{ id: {oldMarket} }}";
                        removeMarkets += "]";
                    }
                    await ExecGraphQLMutationAsync<object>("updateDisplay", $"id: {displayId}, input: {{ addMarkets: [{marketsStr}] {removeMarkets} name: \"{displayName}\" description: \"{desc}\" status: {selVariant.Status} tags: [] addProductVariants: [{productVariantStr}] {removeCategories} addCategories: [ {categIdStr} ] canonicalCategory: {{ id: {canonicalCategId} }} uri: \"{uri}\" addProductMedia: [{mediaStr}] }}");
                }

                if (exceptions.Count > 0)
                {
                    var message = string.Join(',', exceptions.ToArray());
                    throw new Exception(message);
                }
            }
        }

        public async Task PrepareStoresAndMarket()
        {
            _stores = await GetStoresAsync();
            _markets = await GetMarketsAsync();
            _occtooProductSvcImportProd = CreateOcctoProductService();
        }

        public async Task<List<string>> ImportProductFromProductMasterAsync(string productMaster)
        {
            var errorLog = new List<string>() { };
            try
            {
                if (_stores == null || _markets == null || _occtooProductSvcImportProd == null)
                    await PrepareStoresAndMarket();

                var flattenedProductHierarchy = await _occtooProductSvcImportProd.GetProductHierarchyAsync(productMaster);
                if (flattenedProductHierarchy.Count > 0)
                {
                    var collection = (CollectionDto)null;
                    var seasonCode = flattenedProductHierarchy.First().productSeasonCode;
                    collection = await GetCollectionAsync(seasonCode);
                    if (collection == null && !string.IsNullOrEmpty(seasonCode))
                        collection = await CreateCollectionAsync(seasonCode);

                    var productHierarchy = await _occtooProductSvcImportProd.CreateProductHierarchyAsync(flattenedProductHierarchy, collection?.id ?? 0);
                    await ImportProductHierarchyAsync(productHierarchy, _stores, _markets);
                }
                else
                    errorLog.Add($"Product tree is empty for productMaster: [{productMaster}]|{productMaster}");
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case OcctooServerError:
                    case CentraServerError:
                        throw;
                    default:
                        errorLog.Add($"Exception on|{productMaster}|{ex.Message}");
                        break;
                }
            }
            return errorLog;
        }

        private async Task<List<int>> GetDisplayCategoriesAsync(int displayId)
        {
            var result = new List<int>() { };
            if (displayId == 0)
                return result;

            var categories = await GetListOfObjects<DisplayCategoriesDto>("displays", "canonicalCategory { id } categories { id }", $"where: {{ id: {displayId}}}");
            if (categories.Count > 0)
            {
                var categ = categories.FirstOrDefault();

                if (categ?.categories != null)
                    foreach (var category in categ.categories)
                        result.Add(category.id);

                if (categ?.canonicalCategory != null && !result.Contains(categ.canonicalCategory.id))
                    result.Add(categ.canonicalCategory.id);
            }

            return result;
        }

        private async Task<List<int>> GetDisplayMarketsAsync(int displayId)
        {
            var result = new List<int>() { };
            if (displayId == 0)
                return result;

            var markets = await GetListOfObjects<DisplayMarketsDto>("displays", "markets { id }", $"where: {{ id: {displayId}}}");
            if (markets.Count > 0)
            {
                var marketDto = markets.FirstOrDefault();
                if (marketDto?.markets != null)
                    foreach (var market in marketDto.markets)
                        result.Add(market.id);
            }

            return result;
        }
        #endregion

        #region GetProductIdAsync
        public async Task<int> GetProductIdAsync(string productNumber, string status = "ACTIVE")
        {
            var products = await ExecGraphQLQueryAsync<OnlyIdDto>("products", "id", $"where: {{ productNumber: {{ equals: \"{productNumber}\" }}, status: {status} }}");

            return products?.FirstOrDefault()?.id ?? 0;
        }
        #endregion

        #region GetSizesForSizeChartAsync
        public async Task<List<SizeChartWithSizesDto>> GetSizesForSizeChartAsync(int centraSizeChartId) =>
            await GetListOfObjects<SizeChartWithSizesDto>("sizeCharts", "id name sizes { id name }", $"where: {{ id: {centraSizeChartId} }}");
        #endregion

        #region Mapped attributes
        private async Task<int> GetMappedAttributeIdAsync(string name, string attrType, string objectType)
        {
            var result = await ExecGraphQLQueryAsync<OnlyIdDto>("mappedAttributes", "id", $"where: {{ typeName: {{equals: \"{attrType}\"}} name: {{ equals: \"{name}\"}} objectType: {objectType}}}");

            return result?.Count == 1 ? result.First().id : 0;
        }

        private async Task<int> CreateMappedAttributeAsync(string attrName, string attrTypeName, string elementKey, string elementVal)
        {
            var result = await ExecGraphQLMutationAsync<OnlyIdDto>("createAttribute", $"input: {{ name: \"{attrName}\", attributeTypeName: \"{attrTypeName}\", stringElements: {{ key: \"{elementKey}\", value: \"{elementVal}\"  }} }}", "attribute", "id");

            return result?.id ?? 0;
        }

        private async Task<int> CreateColorNameMappedAttributeAsync(string attrName, string attrTypeName, string elementVal)
        {
            var result = await ExecGraphQLMutationAsync<OnlyIdDto>("createAttribute", $"input: {{ name: \"{attrName}\", attributeTypeName: \"{attrTypeName}\", stringElements:[ {{ key: \"name\", value: \"{elementVal}\"  }} {{ key: \"color\", value: \"{elementVal}\"  }} ]}}", "attribute", "id");

            return result?.id ?? 0;
        }

        private async Task AssignMappedAttributeAsync(int objectId, string objectType, string attrTypeName, int attrId) =>
            await ExecGraphQLMutationAsync<object>("assignAttributes", $"input: {{ objectId: {objectId}, objectType: {objectType}, mappedAttributes: {{ attributeTypeName: \"{attrTypeName}\", attributeId: {attrId} }} }}");

        private async Task UnassignMappedAttributeAsync(int objectId, string objectType, string attrTypeName, int attrId) =>
            await ExecGraphQLMutationAsync<object>("unassignAttributes", $"input: {{ objectId: {objectId}, objectType: {objectType}, mappedAttributes: {{ attributeTypeName: \"{attrTypeName}\", attributeId: {attrId} }} }}");

        private async Task<List<MappedAttributeDto>> GetMappedAttributesAsync(string entity, int objectId)
        {
            var result = await ExecGraphQLQueryAsync<MappedAttributesDto>(entity, "attributes { type { name } elements { key ... on AttributeStringElement { value } } }", $"where: {{id: {objectId}}}");

            return result?.Count == 1 ? result.First().attributes?.ToList() ?? new List<MappedAttributeDto>() { } : new List<MappedAttributeDto>() { };
        }
        #endregion

        #region Centra helpers
        private async Task<List<StoreDto>> GetStoresAsync() =>
            await GetListOfObjects<StoreDto>("stores", "id name");

        private async Task<List<MarketDto>> GetMarketsAsync() =>
            await GetListOfObjects<MarketDto>("markets", "id name store { id name }");

        private async Task<CollectionDto> CreateCollectionAsync(string seasonCode) =>
            await ExecGraphQLMutationAsync<CollectionDto>("createCollection", $"input: {{ name: \"{seasonCode}\", status: ACTIVE }}", "collection", "id name");

        private async Task<List<ProductInventoryGraphDto>> GetInventoryForProductMasterAsync(string productMaster) =>
            await ExecGraphQLQueryAsync<ProductInventoryGraphDto>("products", "id variants { id productSizes { id SKU size { id name } } }", $"where: {{ productNumber: {{ equals: \"{productMaster}\" }}, status: ACTIVE }}");

        private async Task<MediaIdsDto> GetMediaDtosForProductVariantAsync(int centraProductVariantId)
        {
            var result = await ExecGraphQLQueryAsync<MediaIdsDto>("productVariants", "id media { id }", $"where: {{ id: [{centraProductVariantId}] }}");

            return result?.FirstOrDefault() ?? new MediaIdsDto() { id = centraProductVariantId, media = Array.Empty<MediaIdDto>() };
        }

        private async Task<int> GetDisplayIdAsync(int productId)
        {
            var display = await ExecGraphQLQueryAsync<DisplayDto>("displays", "id", $"where: {{ productId: {productId}, status: ACTIVE }}");

            return display?.Count == 1 ? display.First().id : 0;
        }

        private async Task<CollectionDto> GetCollectionAsync(string seasonCode)
        {
            var collections = await ExecGraphQLQueryAsync<CollectionDto>("collections", "id name", $"where: {{ name: {{ equals: \"{seasonCode}\" }} }}");

            return collections?.Count == 1 ? collections.First() : null;
        }

        private async Task<ProductVariantQuantityDto> GetVariantQuantityAsync(int variantId)
        {
            var variantQuantities = await ExecGraphQLQueryAsync<ProductVariantQuantityDto>("productVariants", "stock(limit: 200) { productSize { id SKU } availableNowQuantity physicalQuantity }", $"where: {{ id: {variantId} }}");

            return variantQuantities?.FirstOrDefault();
        }

        public async Task<ProductGraphDto> GetProductGraphAsync(int productId)
        {
            if (productId == 0)
                return null;

            var productGraph = await ExecGraphQLQueryAsync<ProductGraphDto>("products", "id variants { id variantNumber productSizes { id sizeNumber SKU } } ", $"where: {{ id: {productId} }}");

            return productGraph?.FirstOrDefault();
        }
        #endregion

        #region GraphQL helpers
        private async Task<List<T>> GetListOfObjects<T>(string entity, string query, string queryParams = "")
        {
            var page = 1;
            var objectList = new List<T>() { };
            var singlePage = (List<T>)null;
            do
            {
                singlePage = await ExecGraphQLQueryAsync<T>(entity, query, $"limit: {LIMIT}, page: {page} {(queryParams != "" ? "," + queryParams : "")}");
                if (singlePage == null || singlePage.Count == 0)
                    break;
                objectList.AddRange(singlePage);
                page += 1;

            } while (singlePage.Count == LIMIT);

            return objectList;
        }

        private async Task<List<T>> ExecGraphQLQueryAsync<T>(string entity, string query, string queryParams = "") =>
            await ExecGraphQLAsync<T>($"query {{ {entity} {(queryParams != "" ? $"({queryParams})" : "")} {{ {query}  }}  }}", entity);

        private async Task<List<T>> ExecGraphQLAsync<T>(string queryStr, string arrayName)
        {
            var tryOut = 1;
            var resultObjects = await ExecGraphInternalQLAsync<T>(queryStr, arrayName);
            while (resultObjects == null)
            {
                resultObjects = await ExecGraphInternalQLAsync<T>(queryStr, arrayName);
                tryOut++;
                if (resultObjects == null && tryOut >= MAX_TRYOUTS)
                    break;
                await Task.Delay(250);
            }
            return resultObjects ?? throw new CentraServerError();
        }
        private async Task<List<T>> ExecGraphInternalQLAsync<T>(string queryStr, string arrayName)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 0, 0));
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_centraAuthToken}");
                var centraRequest = new CentraRequest();
                centraRequest.query = queryStr;
                var body = JsonConvert.SerializeObject(centraRequest);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = new CentraQueryResponse();
                var responseMssg = await httpClient.PostAsync($"{_url}", content, cancellationTokenSource.Token);
                var jsonStr = await responseMssg.Content.ReadAsStringAsync();
                if (jsonStr.StartsWith("<"))
                {
                    return null;
                }

                response = JsonConvert.DeserializeObject<CentraQueryResponse>(jsonStr);
                var result = JObject.Parse(JsonConvert.SerializeObject(response.data));
                var array = (JArray)result.Property($"{arrayName}")?.Value;

                return JsonConvert.DeserializeObject<List<T>>(array.ToString());
            }
            catch
            {
                return null;
            }
        }

        private async Task<T> ExecGraphQLMutationAsync<T>(string functionName, string functionParams = "", string objectName = "", string objectParams = "") =>
            await ExecGraphQLNonQueryAsync<T>($"mutation {{ {functionName} {(functionParams != "" ? $"({functionParams})" : "")} {{ {(objectName != "" ? $"{objectName} {{ {objectParams} }}" : "")} userErrors {{ message }} }} }}", functionName, objectName);

        private async Task<T> ExecGraphQLNonQueryAsync<T>(string mutationStr, string functionName, string objectName = "")
        {
            var tryOut = 1;
            var (resultObject, successfullyDone) = await ExecGraphQLNonQueryInternalAsync<T>(mutationStr, functionName, objectName);
            while (successfullyDone == false)
            {
                (resultObject, successfullyDone) = await ExecGraphQLNonQueryInternalAsync<T>(mutationStr, functionName, objectName);
                tryOut++;
                if (successfullyDone == false && tryOut >= MAX_TRYOUTS)
                    break;
                await Task.Delay(250);
            }
            return successfullyDone ? resultObject : throw new CentraServerError();
        }

        private async Task<(T, bool)> ExecGraphQLNonQueryInternalAsync<T>(string mutationStr, string functionName, string objectName = "")
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 0, 0));
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_centraAuthToken}");
                var centraRequest = new CentraRequest();
                centraRequest.query = mutationStr;
                var body = JsonConvert.SerializeObject(centraRequest);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var responseMssg = await httpClient.PostAsync($"{_url}", content, cancellationTokenSource.Token);
                var jsonStr = await responseMssg.Content.ReadAsStringAsync();
                if (jsonStr.StartsWith("<"))
                {
                    return (default, false);
                }

                var response = JsonConvert.DeserializeObject<CentraMutationResponse>(jsonStr);
                var result = JObject.Parse(JsonConvert.SerializeObject(response.data));
                var mutationObj = (JObject)result.Property(functionName)?.Value;

                if (objectName != "")
                {
                    var obj = (JObject)mutationObj.Property(objectName)?.Value;
                    return (JsonConvert.DeserializeObject<T>(obj.ToString()), true);
                }
                return (default, true);
            }
            catch
            {
                return (default, false);
            }
        }
    }
    #endregion
}
