// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class GearsOfWarQueryRelationalTestBase<TFixture> : GearsOfWarQueryTestBase<TFixture>
        where TFixture : GearsOfWarQueryFixtureBase, new()
    {
        protected GearsOfWarQueryRelationalTestBase(TFixture fixture)
            : base(fixture)
        {
        }

        protected override Dictionary<(Type, string), Func<object, object>> GetShadowPropertyMapping()
        {
            var discriminatorMapping = new Dictionary<(Type, string), Func<object, object>>
            {
                {
                    (typeof(Gear), "Discriminator"),
                    e =>
                    {
                        switch (((Gear)e)?.Nickname)
                        {
                            case "Baird":
                            case "Marcus":
                                return "Officer";

                            case "Cole Train":
                            case "Dom":
                            case "Paduk":
                                return "Gear";

                            default:
                                return null;
                        }
                    }
                },
                {
                    (typeof(Faction), "Discriminator"),
                    e =>
                    {
                        switch (((Faction)e)?.Id)
                        {
                            case 1:
                            case 2:
                                return "LocustHorde";

                            default:
                                return null;
                        }
                    }
                },
                {
                    (typeof(LocustLeader), "Discriminator"),
                    e =>
                    {
                        switch (((LocustLeader)e)?.Name)
                        {
                            case "General Karn":
                            case "General RAAM":
                            case "High Priest Skorge":
                            case "The Speaker":
                                return "LocustLeader";

                            case "Queen Myrrah":
                            case "Unknown":
                                return "LocustCommander";

                            default:
                                return null;
                        }
                    }
                },
            };

            foreach (var shadowPropertyMappingElement in base.GetShadowPropertyMapping())
            {
                discriminatorMapping.Add(shadowPropertyMappingElement.Key, shadowPropertyMappingElement.Value);
            }

            return discriminatorMapping;
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Correlated_collection_with_Distinct_missing_indentifying_columns_in_projection(bool async)
        {
            var message = (await Assert.ThrowsAsync<InvalidOperationException>(
                () => AssertQuery(
                async,
                ss => ss.Set<Gear>()
                    .OrderBy(g => g.Nickname)
                    .Select(g => g.Weapons.SelectMany(x => x.Owner.AssignedCity.BornGears)
                    .Select(x => (bool?)x.HasSoulPatch).Distinct().ToList())))).Message;

            Assert.Equal(RelationalStrings.MissingIdentifyingProjectionInDistinctGroupBySubquery("w.Id"), message);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Correlated_collection_with_GroupBy_missing_indentifying_columns_in_projection(bool async)
        {
            var message = (await Assert.ThrowsAsync<InvalidOperationException>(
                () => AssertQuery(
                async,
                ss => ss.Set<Mission>()
                    .Select(m => new
                    {
                        m.Id,
                        grouping = m.ParticipatingSquads
                            .Select(ps => ps.SquadId)
                            .GroupBy(s => s)
                            .Select(g => new { g.Key, Count = g.Count() })
                    })))).Message;

            Assert.Equal(RelationalStrings.MissingIdentifyingProjectionInDistinctGroupBySubquery("s.MissionId"), message);
        }

        public override async Task Client_eval_followed_by_aggregate_operation(bool async)
        {
            await AssertTranslationFailed(
                () => AssertSum(
                    async,
                    ss => ss.Set<Mission>().Select(m => m.Duration.Ticks)));

            await AssertTranslationFailed(
                () => AssertAverage(
                    async,
                    ss => ss.Set<Mission>().Select(m => m.Duration.Ticks)));

            await AssertTranslationFailed(
                () => AssertMin(
                    async,
                    ss => ss.Set<Mission>().Select(m => m.Duration.Ticks)));

            await AssertTranslationFailed(
                () => AssertMax(
                    async,
                    ss => ss.Set<Mission>().Select(m => m.Duration.Ticks)));
        }

        public override Task Client_member_and_unsupported_string_Equals_in_the_same_query(bool async)
        {
            return AssertTranslationFailedWithDetails(() => base.Client_member_and_unsupported_string_Equals_in_the_same_query(async),
                CoreStrings.QueryUnableToTranslateStringEqualsWithStringComparison
                + Environment.NewLine
                + CoreStrings.QueryUnableToTranslateMember(nameof(Gear.IsMarcus), nameof(Gear)));
        }

        public override Task Client_side_equality_with_parameter_works_with_optional_navigations(bool async)
        {
            return AssertTranslationFailed(() => base.Client_side_equality_with_parameter_works_with_optional_navigations(async));
        }

        public override Task Correlated_collection_order_by_constant_null_of_non_mapped_type(bool async)
        {
            return AssertTranslationFailed(() => base.Correlated_collection_order_by_constant_null_of_non_mapped_type(async));
        }

        public override Task GetValueOrDefault_on_DateTimeOffset(bool async)
        {
            return AssertTranslationFailed(() => base.GetValueOrDefault_on_DateTimeOffset(async));
        }

        public override Task Where_coalesce_with_anonymous_types(bool async)
        {
            return AssertTranslationFailed(() => base.Where_coalesce_with_anonymous_types(async));
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Project_discriminator_columns(bool async)
        {
            await AssertQuery(
                async,
                ss => ss.Set<Gear>().Select(g => new { g.Nickname, Discriminator = EF.Property<string>(g, "Discriminator") }),
                elementSorter: e => e.Nickname);

            await AssertQuery(
                async,
                ss => ss.Set<Gear>().OfType<Officer>().Select(g => new { g.Nickname, Discriminator = EF.Property<string>(g, "Discriminator") }),
                elementSorter: e => e.Nickname);

            await AssertQuery(
                async,
                ss => ss.Set<Faction>().Select(f => new { f.Id, Discriminator = EF.Property<string>(f, "Discriminator") }),
                elementSorter: e => e.Id);

            await AssertQuery(
                async,
                ss => ss.Set<Faction>().OfType<LocustHorde>().Select(lh => new { lh.Id, Discriminator = EF.Property<string>(lh, "Discriminator") }),
                elementSorter: e => e.Id);

            await AssertQuery(
                async,
                ss => ss.Set<LocustLeader>().Select(ll => new { ll.Name, Discriminator = EF.Property<string>(ll, "Discriminator") }),
                elementSorter: e => e.Name);

            await AssertQuery(
                async,
                ss => ss.Set<LocustLeader>().OfType<LocustCommander>().Select(ll => new { ll.Name, Discriminator = EF.Property<string>(ll, "Discriminator") }),
                elementSorter: e => e.Name);
        }

        protected virtual bool CanExecuteQueryString
            => false;

        protected override QueryAsserter CreateQueryAsserter(TFixture fixture)
            => new RelationalQueryAsserter(
                fixture, RewriteExpectedQueryExpression, RewriteServerQueryExpression, canExecuteQueryString: CanExecuteQueryString);
    }
}
