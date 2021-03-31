namespace PusherClient
{
    /// <summary>
    /// Inteface for a Json serializer.
    /// </summary>
    public interface ISerializeObjectsToJson
    {
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="objectToSerialize">The object to be serialized into a Json string.</param>
        /// <returns>The passed in object as a Json string.</returns>
        string Serialize(object objectToSerialize);
    }
}
