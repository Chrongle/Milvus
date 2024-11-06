using Milvus.Client;

string Host = "localhost";
int Port = 19530; // This is Milvus's default port
bool UseSsl = false; // Default value is false

// See documentation for other constructor paramters
MilvusClient milvusClient = new MilvusClient(Host, Port, UseSsl);
MilvusHealthState result = await milvusClient.HealthAsync();

var collectionName = "book";
MilvusCollection collection = milvusClient.GetCollection(collectionName);

//Check if this collection exists
var hasCollection = await milvusClient.HasCollectionAsync(collectionName);

if (hasCollection)
{
    await collection.DropAsync();
    Console.WriteLine("Drop collection {0}", collectionName);
}

var bookIdSchema = "book_id";
var wordCountSchema = "word_count";
var bookNameSchema = "book_name";
var bookIntroSchema = "book_intro";

await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldSchema.Create<long>(bookIdSchema, isPrimaryKey:true),
                FieldSchema.Create<long>(wordCountSchema),
                FieldSchema.CreateFloatVector(bookIntroSchema, 2)
            },
            shardsNum: 2
        );


//Inserting book in collection
await milvusClient.GetCollection(collectionName).InsertAsync(new FieldData[]
{
    FieldData.Create<long>(bookIdSchema, [1]),
    FieldData.Create<long>(wordCountSchema, [300]),
    FieldData.CreateFloatVector(bookIntroSchema, [new[] { 1f, 2f }])
});

await collection.WaitForCollectionLoadAsync()

// Search
List<string> search_output_fields = new() { bookIdSchema };
List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
SearchResults searchResult = await collection.SearchAsync(
    bookIntroSchema,
    new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
    SimilarityMetricType.L2,
    limit: 2);

// Query
var expr = "book_id in [2,4,6,8]";

QueryParameters queryParameters = new ();
queryParameters.OutputFields.Add("book_id");
queryParameters.OutputFields.Add("word_count");

IReadOnlyList<FieldData> queryResult = await collection.QueryAsync(
    expr,
    queryParameters);

Console.WriteLine("SearchResults");
foreach(var res in queryResult)
{
    Console.WriteLine();
    Console.WriteLine(res.FieldName);
    Console.WriteLine();
}