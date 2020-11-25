// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class TPTGearsOfWarQueryRelationalTestBase<TFixture> : GearsOfWarQueryRelationalTestBase<TFixture>
        where TFixture : TPTGearsOfWarQueryRelationalFixture, new()
    {
        protected TPTGearsOfWarQueryRelationalTestBase(TFixture fixture)
            : base(fixture)
        {
        }

        protected override Dictionary<(Type, string), Func<object, object>> GetShadowPropertyMapping()
            => ((GearsOfWarData)Fixture.GetExpectedData())
                .ShadowPropertyMappings.ToDictionary(k => k.Key, e => e.Value);

        [ConditionalTheory(Skip = "issue #22691")]
        public override async Task Cast_to_derived_followed_by_include_and_FirstOrDefault(bool async)
        {
            await base.Cast_to_derived_followed_by_include_and_FirstOrDefault(async);
        }

        public override Task Project_discriminator_columns(bool async)
            => Task.CompletedTask;
    }
}
