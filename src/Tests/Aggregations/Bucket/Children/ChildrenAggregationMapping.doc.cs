﻿using Tests.Framework;
using Tests.Framework.MockData;

namespace Tests.Aggregations.Bucket.Children
{
	public class ChildrenAggregationMapping
	{
		private void MappingExample()
		{
			/** To use the child aggregation you have to make sure 
			 *  a `_parent` mapping is in place, here we create the project
			 *  index with two mapped types, `project` and `commitactivity` and 
			 *  we add a `_parent` mapping from `commitactivity` to `parent` */
			var createProjectIndex = TestClient.GetClient().CreateIndex(typeof(Project), c => c
				.Mappings(map=>map
					.Map<Project>(m=>m.AutoMap())
					.Map<CommitActivity>(m=>m
						.Parent<Project>()
					)
				)
			);
		}
	}
}
