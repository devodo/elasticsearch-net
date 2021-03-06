﻿using System;
using Nest;
using Tests.Framework;
using Tests.Framework.MockData;
using static Nest.Infer;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Aggregations
{
	public class WritingAggregations
	{
		/**
		 * Aggregations are arguably one of the most powerful features of Elasticsearch.
		 * NEST allows you to write your aggregations using a strict fluent dsl, a verbatim object initializer 
		 * syntax that maps verbatim to the elasticsearch API & a more terse object initializer aggregation DSL. 
		 * 
		 * Three different ways, yikes thats a lot to take in! Lets go over them one by one and explain when you might
		 * want to use which one.
		 */
		public class Usage : UsageTestBase<ISearchRequest, SearchDescriptor<Project>, SearchRequest<Project>>
		{
			protected override object ExpectJson => new
			{
				aggs = new
				{
					name_of_child_agg = new
					{
						children = new { type = "commits" },
						aggs = new {
							average_per_child = new
							{
								avg = new { field = "confidenceFactor" }
							},
							max_per_child = new
							{
								max = new { field = "confidenceFactor" }
							}
						}
					}
				}
			};
			/**
			 * The fluent lambda syntax is the most terse way to write aggregations.
			 * It benefits from types that are carried over to sub aggregations
			 */
			protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
				.Aggregations(aggs => aggs
					.Children<CommitActivity>("name_of_child_agg", child => child
						.Aggregations(childAggs => childAggs
							.Average("average_per_child", avg => avg.Field(p => p.ConfidenceFactor))
							.Max("max_per_child", avg => avg.Field(p => p.ConfidenceFactor))
						)
					)
				);

			/**
			 * The object initializer syntax (OIS) is a one-to-one mapping with how aggregations 
			 * have to be represented in the Elasticsearch API. While it has the benefit of being a one-to-one 
			 * mapping, being dictionary based in C# means it can grow exponentially in complexity rather quickly.
			 */
			protected override SearchRequest<Project> Initializer =>
				new SearchRequest<Project>
				{
					Aggregations = new ChildrenAggregation("name_of_child_agg", typeof(CommitActivity))
					{
						Aggregations =
							new AverageAggregation("average_per_child", "confidenceFactor") 
							&& new MaxAggregation("max_per_child", "confidenceFactor")
					}
				};
		}

		public class AggregationDslUsage : Usage
		{
			/**
			 * For this reason the OIS syntax can be shortened dramatically by using `*Agg` related family,
			 * These allow you to forego introducing intermediary Dictionaries to represent the aggregation DSL.
			 * It also allows you to combine multiple aggregations using bitwise AND (`&&`) operator. 
			 * 
			 * Compare the following example with the previous vanilla OIS syntax
			 */
			protected override SearchRequest<Project> Initializer =>
				new SearchRequest<Project>
				{
					Aggregations = new ChildrenAggregation("name_of_child_agg", typeof(CommitActivity))
					{
						Aggregations =
							new AverageAggregation("average_per_child", Field<CommitActivity>(p => p.ConfidenceFactor))
							&& new MaxAggregation("max_per_child", Field<CommitActivity>(p => p.ConfidenceFactor))
					}
				};
		}

		public class AdvancedAggregationDslUsage : Usage
		{
			/**
			 * An advanced scenario may involve an existing collection of aggregation functions that should be set as aggregations 
			 * on the request. Using LINQ's `.Aggregate()` method, each function can be applied to the aggregation descriptor
			 * (`childAggs` below) in turn, returning the descriptor after each function application.
			 *
			 */
			protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent
			{   
				get
				{
					var aggregations = new List<Func<AggregationContainerDescriptor<CommitActivity>, IAggregationContainer>>
					{
						a => a.Average("average_per_child", avg => avg.Field(p => p.ConfidenceFactor)),
						a => a.Max("max_per_child", avg => avg.Field(p => p.ConfidenceFactor))
					};

					return s => s
						.Aggregations(aggs => aggs
							.Children<CommitActivity>("name_of_child_agg", child => child
								.Aggregations(childAggs =>
									aggregations.Aggregate(childAggs, (acc, agg) => { agg(acc); return acc; })
								)
							)
						);
				}
			}
		}
	}
}
