using Milvus.Client;

string host = "localhost";
int port = 19530;
bool useSsl = false;

MilvusClient milvusClient = new MilvusClient(host, port, useSsl);

var collectionName = "book";

//Delete collection if it exists
var hasCollection = await milvusClient.HasCollectionAsync(collectionName);
MilvusCollection collection = milvusClient.GetCollection(collectionName);

if (hasCollection)
{
    await collection.DropAsync();
    Console.WriteLine($"Found existing collection '{collectionName}'");
    Console.WriteLine($"Dropping collection '{collectionName}'");
}

//Schema for collection
var bookIdSchema = "book_id";
var wordCountSchema = "word_count";
var bookNameSchema = "book_name";
var bookVectorSchema = "book_vector";

//Creating collection
Console.WriteLine($"Creating new collection '{collectionName}'");
await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldSchema.Create<long>(bookIdSchema, isPrimaryKey:true),
                FieldSchema.Create<long>(wordCountSchema),
                FieldSchema.CreateVarchar(bookNameSchema, 256), //MaxCount
                FieldSchema.CreateFloatVector(bookVectorSchema, 4) //Number of dimensions
            }
        );

//Creating index for collection
await collection.CreateIndexAsync(
    bookVectorSchema,
    IndexType.AutoIndex,
    SimilarityMetricType.L2);

// Load collection to confirm it
await collection.LoadAsync();


//Inserting books in collection
await milvusClient.GetCollection(collectionName).InsertAsync(new FieldData[]
{
    FieldData.Create<long>(bookIdSchema, [12345, 54321]),
    FieldData.Create<long>(wordCountSchema, [30000, 20100]),
    FieldData.CreateVarChar(bookNameSchema, ["Neuromancer", "Count Zero"]),
    FieldData.CreateFloatVector(bookVectorSchema, [(new[] { 1.2f, 0.3f, 0.2f, 1.8f }), new[] {0.2f, 1.9f, 0.7f, 1.5f}])
});

await milvusClient.GetCollection(collectionName).InsertAsync(new FieldData[]
{
    FieldData.Create<long>(bookIdSchema, [15243]),
    FieldData.Create<long>(wordCountSchema, [100000]),
    FieldData.CreateVarChar(bookNameSchema, ["Mona Lisa Overdrive"]),
    FieldData.CreateFloatVector(bookVectorSchema, [new[] { 2.3f, 1.0f, 1.1f, 1.2f }])
});


//Searching in collection
var results = await milvusClient.GetCollection(collectionName).SearchAsync(
    vectorFieldName: bookVectorSchema,
    vectors: new ReadOnlyMemory<float>[] { new[] { 0f, 0f, 0f, 0f } }, //Vector for searching
    SimilarityMetricType.L2,                                           //Method to calculate nearest vectors
    limit: 10,                                                         //Max amount of vectors
    new()
    {
        ConsistencyLevel = ConsistencyLevel.Strong,
        OutputFields = { wordCountSchema, bookNameSchema }
    });

//Printing out results
Console.WriteLine($"Printing search results from collection '{results.CollectionName}'");
for (int i = 0; i < results.Ids.LongIds?.Count; i++)
{
    var bookId = results.Ids.LongIds[i];
    var wordCount = ((FieldData<long>)results.FieldsData[0]).Data[i];
    var bookName = ((FieldData<string>)results.FieldsData[1]).Data[i];

    Console.WriteLine($"Book ID: {bookId}, Word Count: {wordCount}, Book Name: {bookName}");
}



//Searching nearest book to first book
var nearestResults = await milvusClient.GetCollection(collectionName).SearchAsync(
    vectorFieldName: bookVectorSchema,
    vectors: new ReadOnlyMemory<float>[] { new[] { 1.2f, 0.3f, 0.2f, 1.8f } }, //Vector from first book
    SimilarityMetricType.L2,
    limit: 2,
    new()
    {
        ConsistencyLevel = ConsistencyLevel.Strong,
        OutputFields = { wordCountSchema, bookNameSchema }
    });

//Printing out results
Console.WriteLine($"Getting book and nearest book from collection '{results.CollectionName}'");
for (int i = 0; i < nearestResults.Ids.LongIds?.Count; i++)
{
    var bookId = nearestResults.Ids.LongIds[i];
    var wordCount = ((FieldData<long>)nearestResults.FieldsData[0]).Data[i];
    var bookName = ((FieldData<string>)nearestResults.FieldsData[1]).Data[i];

    Console.WriteLine($"Book ID: {bookId}, Word Count: {wordCount}, Book Name: {bookName}");
}

//Updating a book
Console.WriteLine("Updating book id '15243'");
await milvusClient.GetCollection(collectionName).UpsertAsync(
[
    FieldData.Create<long>(bookIdSchema, [15243]),
    FieldData.Create<long>(wordCountSchema, [100001]),
    FieldData.CreateVarChar(bookNameSchema, ["Virtual Light"]),
    FieldData.CreateFloatVector(bookVectorSchema, [new[] { 3f, 0f, 1f, 2f }])
]);

//Searching in collection
var resultsAfterUpdate = await milvusClient.GetCollection(collectionName).SearchAsync(
    vectorFieldName: bookVectorSchema,
    vectors: new ReadOnlyMemory<float>[] { new[] { 0f, 0f, 0f, 0f } }, 
    SimilarityMetricType.L2,                                           
    limit: 10, //Getting all books                                                         
    new()
    {
        ConsistencyLevel = ConsistencyLevel.Strong,
        OutputFields = { wordCountSchema, bookNameSchema }
    });

//Printing out results
Console.WriteLine($"Printing search results from collection '{resultsAfterUpdate.CollectionName}'");
for (int i = 0; i < resultsAfterUpdate.Ids.LongIds?.Count; i++)
{
    var bookId = resultsAfterUpdate.Ids.LongIds[i];
    var wordCount = ((FieldData<long>)resultsAfterUpdate.FieldsData[0]).Data[i];
    var bookName = ((FieldData<string>)resultsAfterUpdate.FieldsData[1]).Data[i];

    Console.WriteLine($"Book ID: {bookId}, Word Count: {wordCount}, Book Name: {bookName}");
}

//Deleting a book
Console.WriteLine("Deleting first book");
var deleteExpression = $"{bookIdSchema} in [12345]";
await milvusClient.GetCollection(collectionName).DeleteAsync(deleteExpression);

//Searching in collection
var resultsAfterDelete = await milvusClient.GetCollection(collectionName).SearchAsync(
    vectorFieldName: bookVectorSchema,
    vectors: new ReadOnlyMemory<float>[] { new[] { 0f, 0f, 0f, 0f } }, 
    SimilarityMetricType.L2,                                           
    limit: 10, //Getting all books                                                         
    new()
    {
        ConsistencyLevel = ConsistencyLevel.Strong,
        OutputFields = { wordCountSchema, bookNameSchema }
    });

//Printing out results
Console.WriteLine($"Printing search results from collection '{resultsAfterDelete.CollectionName}'");
for (int i = 0; i < resultsAfterDelete.Ids.LongIds?.Count; i++)
{
    var bookId = resultsAfterDelete.Ids.LongIds[i];
    var wordCount = ((FieldData<long>)resultsAfterDelete.FieldsData[0]).Data[i];
    var bookName = ((FieldData<string>)resultsAfterDelete.FieldsData[1]).Data[i];

    Console.WriteLine($"Book ID: {bookId}, Word Count: {wordCount}, Book Name: {bookName}");
}


