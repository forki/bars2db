﻿using System.Collections.Specialized;

namespace LinqToDB.DataProvider.SqlServer
{
    using LinqToDB.Properties;

    [UsedImplicitly]
	class SqlServerFactory : IDataProviderFactory
	{
		#region IDataProviderFactory Implementation

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
			{
				if (attributes.GetKey(i) == "version")
				{
					switch (attributes.Get(i))
					{
						case "2000" : return new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000);
						case "2005" : return new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005);
						case "2012" : return new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012);
						case "2014" : return new SqlServerDataProvider(ProviderName.SqlServer2014, SqlServerVersion.v2012);
					}
				}
			}

			return new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008);
		}

		#endregion
	}
}
