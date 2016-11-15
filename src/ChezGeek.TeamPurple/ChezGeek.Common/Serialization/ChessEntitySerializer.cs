using System;
using Akka.Actor;
using Akka.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace ChezGeek.Common.Serialization
{
    public class ChessEntitySerializer : Serializer
    {
        public ChessEntitySerializer(ExtendedActorSystem system)
            : base(system)
        {
        }

        public override bool IncludeManifest => false;
        public override int Identifier => 1337;

        public override byte[] ToBinary(object obj)
        {
            ThrowIfSerializableAttributeIsMissing(obj);

            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, obj);

                return memoryStream.ToArray();
            }
        }

        private void ThrowIfSerializableAttributeIsMissing(object obj)
        {
            var serializableAttribute = obj.GetType().GetCustomAttribute<SerializableAttribute>();

            if (serializableAttribute == null)
                throw new InvalidOperationException($"The type {obj.GetType().FullName} is missing the SerializableAttribute");
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                var binaryFormatter = new BinaryFormatter();

                return binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
