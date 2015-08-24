using FactualDriver.Exceptions;
using FactualDriver.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FactualDriver.Tests
{
    [TestFixture]
    public class FactualIntegrationTests : FactualIntegrationBase
    {
        public const double Latitude = 34.06018;
        public const double Longitude = -118.41835;
        public const int Meters = 5000;

        [Test]
        public void TestSchema()
        {
            // Arrange
            var result = Factual.Schema("restaurants").Result;
            dynamic json = JsonConvert.DeserializeObject(result);
            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

        [Test]
        public void TestReverseGeocode()
        {
            // Arrange
            var result = Factual.ReverseGeocode(new Point(34.06021, -118.41828)).Result;

            // Assert
            AssertReceivedOkResponse(result);
        }

        [Test]
        public void TestMultiQuery()
        {
            // Arrange
            Factual.QueueFetch("places", new Query().Field("region").Equal("CA"));
            Factual.QueueFetch("places", new Query().Limit(1));
            // Act
            var result = Factual.SendQueueRequests().Result;
            
            // Assert
            dynamic json = JsonConvert.DeserializeObject(result);
            Assert.AreEqual("ok", (string)json.q0.status);
            Assert.AreEqual("ok", (string)json.q1.status);
        }

        [Test]
        public void TestMultiQueryWithCustomKey()
        {
            // Arrange
            Factual.MultiQuery.Key = "test";
            Factual.QueueFetch("places", new Query().Field("region").Equal("CA"));
            Factual.QueueFetch("places", new Query().Limit(1));
            
            // Act
            var result = Factual.SendQueueRequests().Result;

            // Assert
            dynamic json = JsonConvert.DeserializeObject(result);
            Assert.AreEqual("ok", (string)json.test0.status);
            Assert.AreEqual("ok", (string)json.test1.status);
        }

        [Test]
        public void TestMultiComplex()
        {
            // Arrange
            Factual.QueueFetch("global", new FacetQuery("region", "locality"));
            Factual.QueueFetch("places", new Query().Limit(1));
            Factual.QueueFetch("restaurants", new ResolveQuery()
                                             .Add("name", "McDonalds")
                                             .Add("address", "10451 Santa Monica Blvd")
                                             .Add("region", "CA")
                                             .Add("postcode", "90025"));
            // Act
            var response = Factual.SendQueueRequests().Result;

            // Assert
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual("ok", (string)json.q0.status);
            Assert.AreEqual("ok", (string)json.q1.status);
            Assert.AreEqual("ok", (string)json.q2.status);
        }

        [Test]
        public void TestMultiCrosswalk()
        {
            Factual.QueueFetch("crosswalk", new Query()
                                             .Field("factual_id").Equal("c730d193-ba4d-4e98-8620-29c672f2f117")
                                             .Limit(1));
            // Act
            var response = Factual.SendQueueRequests().Result;
            // Assert
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual("ok", (string)json.q0.status);
        }

        [Test]
        public void TestMultiUnicode()
        {
            // Arrange
            Factual.QueueFetch("global", new Query()
                .Field("locality").Equal("大阪市").Limit(5));
            Factual.QueueFetch("global", new Query()
                .Field("locality").Equal("בית שמש").Limit(5));
            Factual.QueueFetch("global", new Query()
                .Field("locality").Equal("München").Limit(5));

            // Act
            var response = Factual.SendQueueRequests().Result;

            // Assert
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual((string)json.q0.response.data[0].locality, "大阪市");
            Assert.AreEqual((int)json.q0.response.included_rows, 5);
            Assert.AreEqual((string)json.q1.response.data[0].locality, "בית שמש");
            Assert.AreEqual((int)json.q1.response.included_rows, 5);
            Assert.AreEqual((string)json.q2.response.data[0].locality, "München");
            Assert.AreEqual((int)json.q2.response.included_rows, 5);
        }

        [Test]
        public void RawQueryTest()
        {
            // Arrange
            var rawParameters = "filters={\"name\":{\"$bw\":\"Star\"}}&include_count=true";

            // Act
            string result = Factual.RawQuery("t/global", rawParameters).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }


        [Test]
        public void QueryWithSimpleRowFilter()
        {
            // Arrange
            var filter = new RowFilter("country", "US");

            // Act
            string result = Factual.Query("t/global", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

        //"filters={\"name\":{\"$bw\":\"Star\"}}"
        [Test]
        public void QueryWithBeginFilter()
        {
            // Arrange
            var filter = new RowFilter("name", "$bw", "$Star");
            
            // Act
            string result = Factual.Query("t/global", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);
            // Assert
            Assert.AreEqual("ok",(string)json.status);
        }

        [Test]
        public void QueryWithGeoFilter()
        {
            // Arrange
            var filter = new GeoFilter(34.06018, -118.41835, 500);

            // Act
            string result = Factual.Query("t/global", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

        [Test]
        public void QueryWithRectangleFilter()
        {
            var filter = new Rectangle(47.622525, -122.353599, 47.618244, -122.340339);

            String result = Factual.Query("t/places", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            Assert.AreEqual("ok", (string)json.status);
            Assert.IsTrue(result.Contains("Space Needle"));
        }

        [Test]
        public void FullTextSearch()    
        {
            // Arrange
            var filter = new SearchFilter("vegan,Los Angeles");

            // Act
            string result = Factual.Query("t/restaurants", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }


        [Test]
        public void MultipleFiltersTest()
        {
            // Arrange
            var filter = new RowFilter("name", "$bw", "Star");
            var filter2 = new Filter("include_count", "true");

            // Act
            string result = Factual.Query("t/restaurants", filter, filter2).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

        [Test]
        public void GeoAndRowFilterTest()
        {
            // Arrange
            var filter = new RowFilter("name", "Stand");
            var filter2 = new GeoFilter(34.06018, -118.41835, 5000);

            // Act
            string result = Factual.Query("t/restaurants", filter, filter2).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }


        [Test]
        public void ParametersWithConditionalOperators()
        {
            // Arrange
            var filter = new FilterGroup(new List<IFilter>
                                                              {
                                                                  new RowFilter("name", "$search", "McDonald's"),
                                                                  new RowFilter("category_labels", "$bw", "Food & Beverage")
                                                              });



            // Act
            string result = Factual.Query("t/global", filter).Result;
            dynamic json = JsonConvert.DeserializeObject(result);

            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

        //Port of factual java driver tests

        [Test]
        public void TestCoreExample1()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query().Field("country").Equal("US")).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        /// <summary>
        /// Find rows in the restaurant database whose name begins with "Star" and
        /// return both the data and a total count of the matched rows.
        /// </summary>
        [Test]
        public void TestCoreExample2()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .Field("name")
                                                       .BeginsWith("Star")).Result;
            var raw = Factual.RawQuery("t/places", "filters={\"name\":{\"$bw\":\"Star\"}}");

            // Assert
            AssertReceivedOkResponse(response);
            Assert.AreEqual(response, raw);
        }

        /// <summary>
        /// Do a full-text search of the restaurant database for rows that match the
        /// terms "Fried Chicken, Los Angeles"
        /// </summary>
        [Test]
        public void TestCoreExample3()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query().Search("Fried Chicken, Los Angeles")).Result;
            Factual.Debug = true;
            var raw = Factual.RawQuery("t/places", "q=Fried Chicken, Los Angeles");
            var test2 = Factual.RawQuery("t/places", "q=Fried Chicken%2C Los Angeles");
            // Assert
            AssertReceivedOkResponse(response);
            Assert.AreEqual(test2, raw);
            Assert.AreEqual(response, raw);
        }

        /// <summary>
        /// Do a full-text search of the the global database with non-english search charachters
        /// terms "משה"
        /// </summary>
        [Test]
        public void TestCoreExample3NonEnglish()
        {
            // Arrange & Act
            var response = Factual.Fetch("global", new Query().Search("משה")).Result;
            Factual.Debug = true;

            // Assert
            AssertReceivedOkResponse(response);
        }

        /// <summary>
        /// To support paging in your app, return rows 6-10 of the full-text search result
        /// from Example 3
        /// </summary>
        [Test]
        public void TestCoreExample4()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                .Search("Fried Chicken,Los Angeles")
                .Offset(5)
                .Limit(5))
                .Result;
            dynamic jsonResponse = JsonConvert.DeserializeObject(response);
            var raw = Factual.RawQuery("t/places", "q=Fried+Chicken,Los+Angeles&offset=5&limit=5");

            // Assert
            AssertReceivedOkResponse(response);
            Assert.AreEqual(5, ((ICollection<JToken>)jsonResponse.response.data).Count);
            Assert.AreEqual(response, raw);
        }

        /// <summary>
        /// Return rows from the global places database with a name equal to "Stand"
        /// within 5000 meters of the specified lat/lng
        /// </summary>
        [Test]
        public void TestCoreExample5()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .Field("name").Equal("The Counter")
                                                       .WithIn(new Circle(Latitude, Longitude, Meters))).Result;

            var raw = Factual.RawQuery("t/places", "filters={\"name\":{\"$eq\":\"The Counter\"}}&geo={\"$circle\":{\"$center\":[34.06018,-118.41835],\"$meters\":5000}}");

            // Assert
            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestSortByDistance()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .WithIn(new Circle(Latitude, Longitude, Meters))
                                                       .SortAsc("$distance")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertAscendingDoubles(response, "$distance");

            var raw = Factual.RawQuery("t/places", "geo={\"$circle\":{\"$center\":[34.06018,-118.41835],\"$meters\":5000}}&sort=$distance:asc");

            // Assert

            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestBlendedSort()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .WithIn(new Circle(Latitude, Longitude, Meters))
                                                       .SortBlendRankAndDistance(100, 50)).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);

            var raw = Factual.RawQuery("t/places", "geo={\"$circle\":{\"$center\":[34.06018,-118.41835],\"$meters\":5000}}&sort={\"placerank\":100,\"distance\":50}");

            // Assert
            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestRowFiltersTwoBeginsWith()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .Field("name").BeginsWith("McDonald's")
                                                       .Field("locality").BeginsWith("Los")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertStartsWith(response, "name", "McDonald");
            AssertStartsWith(response, "locality", "Los");

            var raw = Factual.RawQuery("t/places", "filters={\"$and\":[{\"name\":{\"$bw\":\"McDonald's\"}},{\"locality\":{\"$bw\":\"Los\"}}]}");

            // Assert
            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestIn()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                                                       .Field("region").In("CA", "NM", "FL")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertIn(response, "region", "CA", "NM", "FL");

            var raw = Factual.RawQuery("t/places", "filters={\"region\":{\"$in\":[\"CA\",\"NM\",\"FL\"]}}");

            // Assert

            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestNestedOrWithinTheTopLevelAnd()
        {
            // Arrange & Act
            var query = new Query().Field("region").In("MA", "VT", "NH");
            query.Or
                (
                    query.Field("name").BeginsWith("Coffee"),
                    query.Field("name").BeginsWith("Star")
                );

            var response = Factual.Fetch("places", query).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertStartsWithEither(response, "name", "Coffee", "Star");

            var raw = Factual.RawQuery("t/places", "filters={\"$and\":[{\"region\":{\"$in\":[\"MA\",\"VT\",\"NH\"]}},{\"$or\":[{\"name\":{\"$bw\":\"Coffee\"}},{\"name\":{\"$bw\":\"Star\"}}]}]}");

            // Assert

            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestSimpleTel()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query().Field("tel").BeginsWith("(212)")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertStartsWith(response,"tel","(212)");
        }

        [Test]
        public void TestFullTextSaerchOnAField()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query().Field("name").BeginsWith("Fried Chicken")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);

            dynamic json = JsonConvert.DeserializeObject(response);
            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                var responseValue = (string)value["name"];
                Assert.IsTrue(responseValue.ToLower().Contains("frie") || responseValue.ToLower().Contains("fry")
                    || responseValue.ToLower().Contains("chicken"));
            }
        }

        [Test]
        public void TestCrosswalkEx1()
        {
            // Arrange & Act
            var response = Factual.Fetch("crosswalk", new Query().Field("factual_id").Equal("c730d193-ba4d-4e98-8620-29c672f2f117")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertCrosswalkResult(response, "factual_id", "c730d193-ba4d-4e98-8620-29c672f2f117");
            
        }

        [Test]
        public void TestCrosswalkEx2()
        {
            // Arrange & Act
            var response = Factual.Fetch("crosswalk",
                                         new Query().Field("factual_id").Equal("c730d193-ba4d-4e98-8620-29c672f2f117")).Result;

            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            AssertCrosswalkResult(response, "factual_id", "c730d193-ba4d-4e98-8620-29c672f2f117");
        }

        [Test]
        public void TestCrosswalkEx3()
        {
            // Arrange
            var response = Factual.Fetch("crosswalk", new Query().Field("namespace").Equal("foursquare")
                                                       .Field("namespace_id").Equal("4ae4df6df964a520019f21e3")).Result;
            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
        }


        [Test]
        public void TestCrosswalkLimit()
        {
            // Arrange
            var response = Factual.Fetch("crosswalk", new Query().Field("factual_id").Equal("c730d193-ba4d-4e98-8620-29c672f2f117").Limit(1)).Result;
            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((int)json.response.included_rows) == 1);
        }

        [Test]
        public void TestResolveEx1()
        {
            // Arrange & Act
            var response =
                  Factual.Fetch("restaurants", new ResolveQuery()
                  .Add("name", "McDonalds")
                  .Add("address", "10451 Santa Monica Blvd")
                  .Add("region", "CA")
                  .Add("postcode", "90025"))
                  .Result;
            // Assert
            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
        }

        [Test]
        public void TestResolveEx2()
        {
            // Arrange & Act
            var response =
                  Factual.Fetch("places", new ResolveQuery()
                    .Add("name", "César E. Chávez Library")
                    .Add("locality", "Oakland")
                    .Add("region", "CA")
                    .Add("address", "3301 E 12th St"))
                    .Result;

            // Assert
            AssertReceivedOkResponse(response);
            AssertNotEmpty(response);
        }

        [Test]
        public void TestResolveDebugModeDisabled()
        {
            // Arrange & Act
            var response =
                  Factual.Fetch("restaurants", new ResolveQuery()
                  .Add("name", "McDonalds")
                  .Add("address", "10451 Santa Monica Blvd")
                  .Add("region", "CA")
                  .Add("postcode", "90025")
                  .EnableDebugMode(false))
                  .Result;

            dynamic json = JsonConvert.DeserializeObject(response);

            // Assert
            Assert.AreEqual((string) json.status, "ok");
            Assert.AreEqual((int) json.response.included_rows, 1);
        }

        [Test]
        public void TestResolveDebugModeEnabled()
        {
            // Arrange & Act
            var response =
                  Factual.Fetch("restaurants", new ResolveQuery()
                  .Add("name", "McDonalds")
                  .Add("address", "10451 Santa Monica Blvd")
                  .Add("region", "CA")
                  .Add("postcode", "90025")
                  .EnableDebugMode(true))
                  .Result;

            dynamic json = JsonConvert.DeserializeObject(response);

            // Assert
            Assert.AreEqual((string)json.status, "ok");
            Assert.IsTrue(((int)json.response.included_rows) > 1);
        }

        [Test]
        public void TestMatchFound()
        {
            // Arrange & Act
            string id =
                  Factual.Match("restaurants", new MatchQuery()
                  .Add("name", "McDonalds")
                  .Add("address", "10451 Santa Monica Blvd")
                  .Add("region", "CA")
                  .Add("postcode", "90025"))
                  .Result;

            // Assert
            Assert.AreEqual("c730d193-ba4d-4e98-8620-29c672f2f117", id);
        }

        [Test]
        public void TestMatchNotFound()
        {
            // Arrange & Act
            string id =
                  Factual.Match("restaurants", new MatchQuery()
                  .Add("name", "XYZ")
                  .Add("address", "10451 Santa Monica Blvd")
                  .Add("region", "CA")
                  .Add("postcode", "90025"))
                  .Result;

            // Assert
            Assert.AreEqual(null, id);
        }

        [Test]
        public void TestSelectFieldsOnly()
        {
            // Arrange
            var response = Factual.Fetch("places", new Query().Field("country").Equal("US").Only("address", "country")).Result;
            AssertReceivedOkResponse(response);
            AssertAll(response, "country", "us");

            var raw = Factual.RawQuery("t/places", "filters={\"country\":{\"$eq\":\"US\"}}&select=address,country");

            // Assert

            Assert.AreEqual(response, raw);
        }

        /// <summary>
        /// And should not be used for geo queries.
        /// However, neither should it throw an exception.
        /// </summary>
        [Test]
        public void TestInvalidAnd()
        {
            // Arrange
            var query = new Query();
            query.And
                (
                    query.Field("category_labels").BeginsWith("Food"),
                    query.WithIn(new Circle(Latitude, Longitude, Meters))
                );
            // Act
            var response = Factual.Fetch("places", query).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFacet()
        {
            // Arrange
            var facet = new FacetQuery("region", "locality")
                .Search("Starbucks")
                .MaxValuesPerFacet(20)
                .MinCountPerFacetValue(100)
                .IncludeRowCount();

            // Act
            var response = Factual.Fetch("global", facet).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((int)json.response.total_row_count) > 0);
        }

        [Test]
        public void TestFacetFilter()
        {
            // Arrange
            var facet = new FacetQuery("locality")
                .Field("region").In("MA", "VT", "NH");
            facet.And
                (
                    facet.Or
                        (
                            facet.Field("name").BeginsWith("Coffee"),
                            facet.Field("name").BeginsWith("Star")
                        ),
                    facet.Field("locality").BeginsWith("w")
                );
            // Act
            var response = Factual.Fetch("global", facet).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((ICollection<JToken>)json.response.data.locality).Count > 0);
        }

        [Test]
        public void TestFacetGeo()
        {
            // Arrange
            var facet = new FacetQuery("category_labels").Within(new Circle(Latitude, Longitude, Meters));
            // Act
            var response = Factual.Fetch("global", facet).Result;
            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((ICollection<JToken>)json.response.data.category_labels).Count > 0);
        }

        [Test]
        public void TestGeocode()
        {
            // Arrange
            var response = Factual.ReverseGeocode(new Point(Latitude, Longitude)).Result;
            dynamic json = JsonConvert.DeserializeObject(response);
            // Assert
            AssertReceivedOkResponse(response);

            Assert.IsTrue(((ICollection<JToken>) json.response.data).Count > 0);
        }

        [Test]
        public void TestWorldGeographies()
        {
            // Arrange
            var query = new Query();
                query.And
                (
                    query.Field("name").Equal("philadelphia"),
                    query.Field("country").Equal("us"),
                    query.Field("placetype").Equal("locality")
                );
            // Act
                var response = Factual.Fetch("world-geographies", query).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((int)json.response.included_rows) == 14);
        }

        [Test]
        public void TestFlagDuplicate()
        {
            // Arrange
            var response = Factual.FlagDuplicate("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", null, new Metadata().User("test_driver_user")).Result;
           
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagRelocated() {
            var response = Factual.FlagRelocated("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", "21EC2020-3AEA-1069-A2DD-08002B30309D", new Metadata().User("test_driver_user")).Result;

            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagInaccurate()
        {
            // Arrange
            var response = Factual.FlagInaccurate("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new List<String> { "name", "hours" }, new Metadata().User("test_driver_user")).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagInappropriate()
        {
            // Arrange
            var response = Factual.FlagInappropriate("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new Metadata().User("test_driver_user")).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagNonExistent()
        {
            // Arrange
            var response = Factual.FlagNonExistent("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new Metadata().User("test_driver_user")).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagSpam()
        {
            // Arrange
            var response = Factual.FlagSpam("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new Metadata().User("test_driver_user")).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagClosed()
        {
            // Arrange
            var response = Factual.FlagClosed("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new Metadata().User("test_driver_user")).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestFlagOther()
        {
            // Arrange
            var response = Factual.FlagOther("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", new Metadata().User("test_driver_user")).Result;
            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestDiffsResponse()
        {
            // Arrange
            DiffsQuery diffs = new DiffsQuery()
                .After(1403106402094)
                .Before(1403106404094);

            // Act
            var response = Factual.Fetch("places-us", diffs);
            var raw = Factual.RawQuery("t/places-us/diffs?start=1403106402094&end=1403106404094");

            // Assert
            Assert.AreEqual(response, raw);
        }

        [Test]
        [ExpectedException(typeof(FactualException))]
        public void TestDiffsNoResponse()
        {
            // Arrange
            DiffsQuery diffs = new DiffsQuery()
                .After(1304916463822)
                .Before(1304917903834);

            // Act
            var response = Factual.Fetch("places-us", diffs);
        }

        [Test]
        public void TestSubmitAdd()
        {
            CreateNewEntity();
        }

        /// <summary>
        /// http://support.factual.com/factual/topics/submit_api_using_c_driver
        /// </summary>
        [Test]
        public void SubmitAddTestCase2()
        {
            // Arrange
            Submit values = new Submit();

            values.AddValue("name", "Starbucks");
            values.AddValue("address", "72 Spring St");
            values.AddValue("locality", "New York");
            values.AddValue("region", "NY");
            values.AddValue("postcode", "10012");
            values.AddValue("country", "US");
            
            // Act
            Metadata metadata = new Metadata().User("test_driver_user");
            var response = Factual.Submit("us-sandbox", values, metadata).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void SubmitQueryMatchesRawTest()
        {
            Submit submit = new Submit();

            submit.AddValue("name", "McDenny’s");
            submit.AddValue("address", "1 Main St.");
            submit.AddValue("locality", "Bedrock");
            submit.AddValue("region", "BC");
            
            // Assert

            Assert.AreEqual("values={\"name\":\"McDenny’s\",\"address\":\"1 Main St.\",\"locality\":\"Bedrock\",\"region\":\"BC\"}", HttpUtility.UrlDecode(submit.ToUrlQuery()));
        }


        public string CreateNewEntity()
        {
            // Arrange
            Submit submit = new Submit()
                .AddValue("longitude", 100);
            var response = Factual.Submit("us-sandbox", submit, new Metadata().User("test_driver_user")).Result;

            // Asert
            AssertReceivedOkResponse(response);
            dynamic newEntityjson = JsonConvert.DeserializeObject(response);
            var newEntityId = (string)newEntityjson.response.factual_id;
            return newEntityId;
        }

        [Test]
        public void TestSubmitEdit()
        {
            var newEntityId = CreateNewEntity();

            // Arrange
            Submit submit = new Submit()
                .AddValue("longitude", 101);
            var response = Factual.Submit("us-sandbox", newEntityId, submit, new Metadata().User("test_driver_user")).Result;
            
            // Asert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsFalse((bool)json.response.new_entity);
        }

        [Test]
        public void TestSubmitDelete()
        {
            var newEntityId = CreateNewEntity();
            // Arrange
            Submit submit = new Submit()
                .RemoveValue("longitude");
            var response = Factual.Submit("us-sandbox", newEntityId, submit, new Metadata().User("test_driver_user")).Result;

            // Asert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsFalse((bool)json.response.new_entity);
        }

        [Test]
        public void ClearDefaultTest()
        {
            // Arrange
            Clear clear = new Clear();

            // Act
            clear.AddField("name");
            clear.AddField("address");
            clear.AddField("locality");
            clear.AddField("region");

            // Assert
            Assert.AreEqual("fields=name,address,locality,region", HttpUtility.UrlDecode(clear.ToUrlQuery()));
        }

        [Test]
        public void ClearOverloadedTest()
        {
            // Arrange & Act
            Clear clear = new Clear("name", "address", "locality", "region");

            // Assert
            Assert.AreEqual("fields=name,address,locality,region", HttpUtility.UrlDecode(clear.ToUrlQuery()));
        }

        [Test]
        public void ClearOverloadedWithAddTest()
        {
            // Arrange & Act
            Clear clear = new Clear("name", "address", "locality");
            clear.AddField("region");

            // Assert
            Assert.AreEqual("fields=name,address,locality,region", HttpUtility.UrlDecode(clear.ToUrlQuery()));
        }

        [Test]
        public void ClearIntegrationTest()
        {
            // Arrange
            Clear clear = new Clear("latitude", "longitude");
            
            // Act
            var response = Factual.Clear("us-sandbox", "4e4a14fe-988c-4f03-a8e7-0efc806d0a7f", clear,
                                         new Metadata().User("test_driver_user")).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestIncludesRowFilter()
        {
            // Arrange
            var response = Factual.Fetch("places", new Query().Field("category_ids").Includes(10)).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestIncludesAnyRowFilter()
        {
            // Arrange
            var response = Factual.Fetch("places", new Query().Field("category_ids").IncludesAny(10, 100)).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestRowQueryResponse()
        {
            // Arrange
            var response = Factual.FetchRow("places", "03c26917-5d66-4de9-96bc-b13066173c65").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestRowQueryOnlyResponse()
        {
            // Arrange
            var rowQuery = new RowQuery();
            rowQuery.Only("name", "address", "region", "postcode");
            var response = Factual.FetchRow("places", "03c26917-5d66-4de9-96bc-b13066173c65", rowQuery).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestTimeouts()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                .Field("name")
                .BeginsWith("Star"))
                .Result;
            Factual.ConnectionTimeout = 2500;
            Factual.ReadTimeout = 7500;
            var raw = Factual.RawQuery("t/places", "filters={\"name\":{\"$bw\":\"Star\"}}").Result;

            // Assert
            AssertReceivedOkResponse(response);
            AssertReceivedOkResponse(raw);
            Assert.AreEqual(response, raw);
        }

        [Test]
        //[ExpectedException(typeof(System.NullReferenceException))]
        public void TestApiUrlOverride1()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                .Field("name")
                .BeginsWith("Star"))
                .Result;
            //Override FactualApiUrl
            //Factual.FactualApiUrlOverride = "http://fakeurl.factual.com";
            //Reset FactualApiUrl back to default
            //Factual.FactualApiUrlOverride = null;
            var raw = Factual.RawQuery("t/places", "filters={\"name\":{\"$bw\":\"Star\"}}").Result;

            // Assert
            AssertReceivedOkResponse(response);
            AssertReceivedOkResponse(raw);
            Assert.AreEqual(response, raw);
        }

        [Test]
        [ExpectedException(typeof(System.NullReferenceException))]
        public void TestApiUrlOverride2()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query()
                .Field("name")
                .BeginsWith("Star")
                .IncludeRowCount())
                .Result;
            Factual.FactualApiUrlOverride = "http://fakeurl.factual.com";
            var result = Factual.RawQuery("t/places", "filters={\"name\":{\"$bw\":\"Star\"}}&include_count=true").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestNewRawGetSimple()
        {
            // Arrange & Act
            var response = Factual.RawQuery("t/places", "select=name,category_labels&include_count=true").Result;
            var raw = Factual.RawQuery("t/places", new Dictionary<string, object>
                {
                    {"select", "name,category_labels"},
                    {"include_count", "true"}
                }).Result;

            // Assert
            AssertReceivedOkResponse(response);
            AssertReceivedOkResponse(raw);
            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestNewRawGetComplex()
        {
			// Arrange
			var CategoryLabel = "Food & Beverage";
			var Offset = 0;
			var Lat = 34.06018;
			var Lng = -118.41835;
			var Radius = 5000;

			// Act
			var result = Factual.RawQuery("t/places", new Dictionary<string, object>
				{
					{
						"filters", new Dictionary<string, object>
						{
							{
								"category_labels", CategoryLabel
							}
						}
					},
					{
						"limit", 5
					},
					{
						"offset", Offset
					},
					{
						"include_count", (Offset == 0).ToString()
					},
					{
						"geo", new Dictionary<string, object>
						{
							{
								"$circle", new Dictionary<string, object>
								{
									{
										"$center", "[" + Lat + "," + Lng + "]"
									},
									{
										"$meters", Radius
									}
								}
							}
						}
					}
				}
            ).Result;

            var raw = Factual.RawQuery("t/places", "filters={\"category_labels\":\"Food %26 Beverage\"}&limit=5&offset=0&include_count=true&geo={\"$circle\":{\"$center\":[34.06018,-118.41835],\"$meters\":5000}}").Result;

			// Assert
			AssertReceivedOkResponse(result);
			AssertReceivedOkResponse(raw);
			Assert.AreEqual(result, raw);
		}

        [Test]
        public void TestNewRawPostSubmit()
        {
            // Arrange & Act
            var raw = Factual.RequestPost("/t/us-sandbox/submit",
                new Dictionary<string, object>
                {
                    {
                        "values", new Dictionary<string, object>
                        {
                            {
                                "name", "Factual North"
                            },
                            {
                                "address", "1 North Pole"
                            },
                            {
                                "latitude", 90
                            },
                            {
                                "longitude", 0
                            }
                        }
                    },
                    {
                        "user", "test_driver_user"
                    }
                },
                new Dictionary<string, object>()).Result;

            // Assert
            AssertReceivedOkResponse(raw);
        }

        [Test]
        public void TestBasicUnicodeJapanese()
        {
            // Arrange & Act
            var response = Factual.Fetch("global", new Query()
                .Field("locality").Equal("大阪市").Limit(5)).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual((string)json.response.data[0].locality, "大阪市");
            Assert.AreEqual((int)json.response.included_rows, 5);
        }

        [Test]
        public void TestBasicUnicodeHebrew()
        {
            // Arrange & Act
            var response = Factual.Fetch("global", new Query()
                .Field("locality").Equal("בית שמש").Limit(5)).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual((string)json.response.data[0].locality, "בית שמש");
            Assert.AreEqual((int)json.response.included_rows, 5);
        }

        [Test]
        public void TestBasicUnicodeGerman()
        {
            // Arrange & Act
            var response = Factual.Fetch("global", new Query()
                .Field("locality").Equal("München").Limit(5)).Result;

            // Assert
            AssertReceivedOkResponse(response);
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.AreEqual((string)json.response.data[0].locality, "München");
            Assert.AreEqual((int)json.response.included_rows, 5);
        }

        [Test]
        public void TestSearchExact()
        {
            // Arrange & Act
            var response = Factual.Fetch("places", new Query().SearchExact("a b c")).Result;
            var raw = Factual.RawQuery("t/places", "q=\"a b c\"").Result;

            // Assert
            AssertReceivedOkResponse(response);
            Assert.AreEqual(response, raw);
        }

        [Test]
        public void TestBoost()
        {
            // Arrange & Act
            var response = Factual.Boost("us-sandbox", "03c26917-5d66-4de9-96bc-b13066173c65", "Local Business Data, Global", "test_driver_user").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestBoostNoUser()
        {
            // Arrange & Act
            var response = Factual.Boost("us-sandbox", "03c26917-5d66-4de9-96bc-b13066173c65", "Local Business Data, Global").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestBoostNoUserNoSearch()
        {
            // Arrange & Act
            var response = Factual.Boost("us-sandbox", "03c26917-5d66-4de9-96bc-b13066173c65").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestBoostObject()
        {
            // Arrange
            Boost boost = new Boost("03c26917-5d66-4de9-96bc-b13066173c65");
            boost.Search("Local Business Data, Global");
            boost.User("test_driver_user");

            // Act
            var response = Factual.Boost("us-sandbox", boost).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestBoostExistingQuery()
        {
            // Arrange
            Query query = new Query().Search("Local Business Data, Global");
            Metadata metadata = new Metadata().User("test_driver_user");

            // Act
            var response = Factual.Boost("us-sandbox", "03c26917-5d66-4de9-96bc-b13066173c65", query, metadata).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        public void TestDeprecatedEntity()
        {
            // Arrange & Act
            var response = Factual.FetchRow("places", "15442594-6f41-4ba3-9c02-b4ca6e663fcd").Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        [Test]
        [ExpectedException(typeof(FactualApiException))]
        public void TestSubmitError()
        {
            // Arrange
            Submit submit = new Submit().RemoveValue("longitude");

            // Act
            var response = Factual.Submit("us-sandbox", "randomwrongid", submit, new Metadata().User("test_driver_user")).Result;

            // Assert
            AssertReceivedOkResponse(response);
        }

        private void AssertAll(string response, string key, string valueToCheck)
        {
            dynamic json = JsonConvert.DeserializeObject(response);
            Assert.IsTrue(((ICollection<JToken>) json.response.data).All(p => ((string) p[key]).ToLower() == valueToCheck.ToLower()));

        }

        private void AssertCrosswalkResult(string response, string key, string id)
        {
            dynamic json = JsonConvert.DeserializeObject(response);
            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                var responseValue = (string)value[key];
                Assert.IsTrue(responseValue == id);
            }
        }

        private void AssertStartsWithEither(string response, string key, string value1, string value2)
        {
            dynamic json = JsonConvert.DeserializeObject(response);
            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                var responseValue = (string) value[key];
                Assert.IsTrue(responseValue.StartsWith(value1) || responseValue.StartsWith(value2));
            }
        }

        private void AssertIn(string response, string key, params string[] values)
        {
            dynamic json = JsonConvert.DeserializeObject(response);
            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                Assert.IsTrue(values.Contains((string)value[key]));
            }
        }

        private void AssertStartsWith(string response, string key, string valueToCheck)
        {
            dynamic json = JsonConvert.DeserializeObject(response);
            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                Assert.IsTrue(((string)value[key]).StartsWith(valueToCheck, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void AssertAscendingDoubles(string response, string valueToCheck)
        {
            double previous = 0;
            dynamic json = JsonConvert.DeserializeObject(response);

            foreach (var value in (ICollection<JToken>)json.response.data)
            {
                var currentValue = (double) value[valueToCheck];
                Assert.IsTrue(currentValue >= previous);
                previous = currentValue;
            }
        }

        public void  AssertReceivedOkResponse(string result)
        {
            dynamic json = JsonConvert.DeserializeObject(result);
            // Assert
            Assert.AreEqual("ok", (string)json.status);
        }

		public void AssertReceivedWarningResponse(string result)
		{
			dynamic json = JsonConvert.DeserializeObject(result);
			// Assert
			Assert.AreEqual("warning", (string) json.status);
		}

		public void AssertNotEmpty(string result)
        {
            dynamic json = JsonConvert.DeserializeObject(result);
            Assert.IsTrue(((int)json.response.included_rows) > 0);
        }
    }
}
