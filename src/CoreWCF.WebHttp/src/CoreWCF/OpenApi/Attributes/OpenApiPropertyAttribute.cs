﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace CoreWCF.OpenApi.Attributes
{
    /// <summary>
    /// Attribute to denote a information about a property in a request or response object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OpenApiPropertyAttribute : Attribute
    {
        /// <summary>
        /// Description of the property.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether the property is required or not.
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
