﻿using RoverMob.Protocol;
using RoverMob.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RoverMob.Implementation;

namespace RoverMob.Messaging
{
    public class FileMessageStore : Process, IMessageStore
    {
        private readonly string _folderName;

        private JsonSerializer _serializer = new JsonSerializer();

        public FileMessageStore(string folderName)
        {
            _folderName = folderName;
        }

        public Task<ImmutableList<Message>> LoadAsync(Guid objectId)
        {
            var completion = new TaskCompletionSource<ImmutableList<Message>>();
            Perform(() => LoadInternalAsync(objectId, completion));
            return completion.Task;
        }

        public void Save(Message message)
        {
            Perform(() => SaveInternalAsync(message));
        }

        public Task<Guid?> GetUserIdentifierAsync(string role)
        {
            var completion = new TaskCompletionSource<Guid?>();
            Perform(() => GetUserIdentifierInternalAsync(role, completion));
            return completion.Task;
        }

        public void SaveUserIdentifier(string role, Guid userIdentifier)
        {
            Perform(() => SaveUserIdentifierInternalAsync(role, userIdentifier));
        }

        private async Task LoadInternalAsync(Guid objectId, TaskCompletionSource<ImmutableList<Message>> completion)
        {
            try
            {
                string fileName = String.Format("obj_{0}.json",
                    objectId.ToCanonicalString());
                var stream = await FileImplementation.OpenForRead(
                    _folderName, fileName);
                var messages = ReadMessages(stream);
                var result = messages
                    .Select(m => Message.FromMemento(m))
                    .ToImmutableList();
                completion.SetResult(result);
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }

        private async Task SaveInternalAsync(Message message)
        {
            string fileName = String.Format("obj_{0}.json",
                message.ObjectId.ToCanonicalString());
            var inputStream = await FileImplementation.OpenForRead(
                _folderName, fileName);
            var messages = ReadMessages(inputStream);
            if (!messages.Any(m => m.Hash == message.Hash.ToString()))
            {
                messages.Add(message.GetMemento());
                var outputStream = await FileImplementation.OpenForWrite(
                    _folderName, fileName);
                WriteMessages(outputStream, messages);
            }
        }

        private async Task SaveUserIdentifierInternalAsync(string role, Guid userIdentifier)
        {
            string fileName = String.Format("user_{0}.txt", role);
            var outputStream = await FileImplementation.OpenForWrite(
                _folderName, fileName);
            using (var writer = new StreamWriter(outputStream))
            {
                await writer.WriteAsync(userIdentifier.ToCanonicalString());
            }
        }

        private async Task GetUserIdentifierInternalAsync(string role, TaskCompletionSource<System.Guid?> completion)
        {
            try
            {
                string fileName = String.Format("user_{0}.txt", role);
                var inputStream = await FileImplementation.OpenForRead(
                    _folderName, fileName);
                using (var reader = new StreamReader(inputStream))
                {
                    string line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        completion.SetResult(null);
                    else
                        completion.SetResult(Guid.Parse(line));
                }
            }
            catch (Exception x)
            {
                completion.SetException(x);
            }
        }

        private List<MessageMemento> ReadMessages(Stream inputStream)
        {
            List<MessageMemento> messageList;
            using (JsonReader reader = new JsonTextReader(new StreamReader(inputStream)))
            {
                messageList = _serializer.Deserialize<List<MessageMemento>>(reader);
            }

            if (messageList == null)
                messageList = new List<MessageMemento>();
            return messageList;
        }

        private void WriteMessages(Stream outputStream, List<MessageMemento> messageList)
        {
            using (JsonWriter writer = new JsonTextWriter(new StreamWriter(outputStream)))
            {
                _serializer.Serialize(writer, messageList);
            }
        }
    }
}
