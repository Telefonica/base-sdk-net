using System.Web.Script.Serialization;

namespace BaseSdkNetTest
{
    internal class JsonBodyExample
    {
        public string name;
        public string lastName;

        public JsonBodyExample()
        {
            name = "Test";
            lastName = "Api Sdk";
        }

        public string GetJsonEncoded()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(this);
        }
    }
}