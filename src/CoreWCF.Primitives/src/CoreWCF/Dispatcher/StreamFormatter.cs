﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Diagnostics;
using CoreWCF.Runtime;

namespace CoreWCF.Dispatcher
{
    internal class StreamFormatter
    {
        private readonly int _streamIndex;
        private readonly bool _isRequest;
        private readonly string _operationName;
        private const int returnValueIndex = -1;

        internal static StreamFormatter Create(MessageDescription messageDescription, string operationName, bool isRequest)
        {
            MessagePartDescription streamPart = ValidateAndGetStreamPart(messageDescription, isRequest, operationName);
            if (streamPart == null)
            {
                return null;
            }

            return new StreamFormatter(messageDescription, streamPart, operationName, isRequest);
        }

        private StreamFormatter(MessageDescription messageDescription, MessagePartDescription streamPart, string operationName, bool isRequest)
        {
            if ((object)streamPart == (object)messageDescription.Body.ReturnValue)
            {
                _streamIndex = returnValueIndex;
            }
            else
            {
                _streamIndex = streamPart.Index;
            }

            WrapperName = messageDescription.Body.WrapperName;
            WrapperNamespace = messageDescription.Body.WrapperNamespace;
            PartName = streamPart.Name;
            PartNamespace = streamPart.Namespace;
            _isRequest = isRequest;
            _operationName = operationName;
        }

        internal void Serialize(XmlDictionaryWriter writer, object[] parameters, object returnValue)
        {
            Stream streamValue = GetStreamAndWriteStartWrapperIfNecessary(writer, parameters, returnValue);
            var streamProvider = new OperationStreamProvider(streamValue);
            StreamFormatterHelper.WriteValue(writer, streamProvider);
            WriteEndWrapperIfNecessary(writer);
        }

        internal async Task SerializeAsync(XmlDictionaryWriter writer, object[] parameters, object returnValue)
        {
            using (TaskHelpers.RunTaskContinuationsOnOurThreads()) // If inner stream doesn't have sync implementation, don't continue on thread pool.
            {
                // TODO: For NetStandard 2.0, use async methods on writer
                Stream streamValue = GetStreamAndWriteStartWrapperIfNecessary(writer, parameters, returnValue);
                var streamProvider = new OperationStreamProvider(streamValue);
                await StreamFormatterHelper.WriteValueAsync(writer, streamProvider);
                await WriteEndWrapperIfNecessaryAsync(writer);
            }
        }

        private Stream GetStreamAndWriteStartWrapperIfNecessary(XmlDictionaryWriter writer, object[] parameters, object returnValue)
        {
            Stream streamValue = GetStreamValue(parameters, returnValue);
            if (streamValue == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(PartName);
            }

            if (WrapperName != null)
            {
                writer.WriteStartElement(null, WrapperName, WrapperNamespace);
            }

            writer.WriteStartElement(null, PartName, PartNamespace);
            return streamValue;
        }

        private void WriteEndWrapperIfNecessary(XmlDictionaryWriter writer)
        {
            writer.WriteEndElement();
            if (WrapperName != null)
            {
                writer.WriteEndElement();
            }
        }

        private Task WriteEndWrapperIfNecessaryAsync(XmlDictionaryWriter writer)
        {
            writer.WriteEndElement();
            if (WrapperName != null)
            {
                writer.WriteEndElement();
            }

            return Task.CompletedTask;
        }

        internal void Deserialize(object[] parameters, ref object retVal, Message message)
        {
            SetStreamValue(parameters, ref retVal, new MessageBodyStream(message, WrapperName, WrapperNamespace, PartName, PartNamespace, _isRequest));
        }

        internal string WrapperName { get; set; }

        internal string WrapperNamespace { get; set; }

        internal string PartName { get; }

        internal string PartNamespace { get; }

        private Stream GetStreamValue(object[] parameters, object returnValue)
        {
            if (_streamIndex == returnValueIndex)
            {
                return (Stream)returnValue;
            }

            return (Stream)parameters[_streamIndex];
        }

        private void SetStreamValue(object[] parameters, ref object returnValue, Stream streamValue)
        {
            if (_streamIndex == returnValueIndex)
            {
                returnValue = streamValue;
            }
            else
            {
                parameters[_streamIndex] = streamValue;
            }
        }

        private static MessagePartDescription ValidateAndGetStreamPart(MessageDescription messageDescription, bool isRequest, string operationName)
        {
            MessagePartDescription part = GetStreamPart(messageDescription);
            if (part != null)
            {
                return part;
            }

            if (HasStream(messageDescription))
            {
                if (messageDescription.IsTypedMessage)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.SFxInvalidStreamInTypedMessage, messageDescription.MessageName)));
                }
                else if (isRequest)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.SFxInvalidStreamInRequest, operationName)));
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.SFxInvalidStreamInResponse, operationName)));
                }
            }
            return null;
        }

        private static bool HasStream(MessageDescription messageDescription)
        {
            if (messageDescription.Body.ReturnValue != null && messageDescription.Body.ReturnValue.Type == typeof(Stream))
            {
                return true;
            }

            foreach (MessagePartDescription part in messageDescription.Body.Parts)
            {
                if (part.Type == typeof(Stream))
                {
                    return true;
                }
            }
            return false;
        }

        private static MessagePartDescription GetStreamPart(MessageDescription messageDescription)
        {
            if (OperationFormatter.IsValidReturnValue(messageDescription.Body.ReturnValue))
            {
                if (messageDescription.Body.Parts.Count == 0)
                {
                    if (messageDescription.Body.ReturnValue.Type == typeof(Stream))
                    {
                        return messageDescription.Body.ReturnValue;
                    }
                }
            }
            else
            {
                if (messageDescription.Body.Parts.Count == 1)
                {
                    if (messageDescription.Body.Parts[0].Type == typeof(Stream))
                    {
                        return messageDescription.Body.Parts[0];
                    }
                }
            }
            return null;
        }

        internal static bool IsStream(MessageDescription messageDescription)
        {
            return GetStreamPart(messageDescription) != null;
        }

        internal class OperationStreamProvider //: IStreamProvider
        {
            private readonly Stream _stream;

            internal OperationStreamProvider(Stream stream)
            {
                _stream = stream;
            }

            public Stream GetStream()
            {
                return _stream;
            }
            public void ReleaseStream(Stream stream)
            {
                //Noop
            }
        }

        internal class StreamFormatterHelper
        {
            // The method was duplicated from the desktop implementation of
            // System.Xml.XmlDictionaryWriter.WriteValue(IStreamProvider)
            public static void WriteValue(XmlDictionaryWriter writer, OperationStreamProvider value)
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
                }

                Stream stream = value.GetStream();
                if (stream == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.XmlInvalidStream)));
                }

                int blockSize = 256;
                byte[] block = new byte[blockSize];
                while (true)
                {
                    int bytesRead = stream.Read(block, 0, blockSize);
                    if (bytesRead > 0)
                    {
                        writer.WriteBase64(block, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }

                    if (blockSize < 65536 && bytesRead == blockSize)
                    {
                        blockSize = blockSize * 16;
                        block = new byte[blockSize];
                    }
                }

                value.ReleaseStream(stream);
            }

            internal static async Task WriteValueAsync(XmlDictionaryWriter writer, OperationStreamProvider value)
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(value)));
                }

                Stream stream = value.GetStream();
                if (stream == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.XmlInvalidStream));
                }

                int blockSize = 256;
                byte[] block = new byte[blockSize];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(block, 0, blockSize);
                    if (bytesRead > 0)
                    {
                        // XmlDictionaryWriter has not implemented WriteBase64Async() yet.
                        writer.WriteBase64(block, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }

                    if (blockSize < 65536 && bytesRead == blockSize)
                    {
                        blockSize = blockSize * 16;
                        block = new byte[blockSize];
                    }
                }

                value.ReleaseStream(stream);
            }
        }
    }
}
