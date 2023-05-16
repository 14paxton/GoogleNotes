using System;
using System.Collections.Generic;

namespace Comm100API
{
	public class InteractionResponse
	{

		public Output output { get; set; }

		public class Content
		{
			public string type { get; set; }
			public Content content { get; set; }
			public decimal delaytime { get; set; }
			public List<Item> items { get; set; }
			public string message { get; set; }
			public List<Link> links { get; set; }
			public Guid transferTo { get; set; }
			public string messageWhenAgentOffline { get; set; }
		}

		public class Item
		{
			public string buttonText { get; set; }
			public string url { get; set; }
			public string type { get; set; }
			public string openStyle { get; set; }
			public string openIn { get; set; }
			public int order { get; set; }
		}

		public class Link
		{
			public string buttonText { get; set; }
			public string url { get; set; }
			public string type { get; set; }
			public string openStyle { get; set; }
			public string openIn { get; set; }
			public int order { get; set; }
		}

		public class Output
		{
			public string id { get; set; }
			public List<Content> content { get; set; }
		}
	}
}