using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fpl.Api.Tools
{
    public class CacheProxy<T> : DispatchProxy
    {
        private T _service;
        private IMemoryCache _memoryCache;
        
        private void SetParameters(T service, IMemoryCache memoryCache)
        {
            _service = service;
            _memoryCache = memoryCache;
        }
        public static T Create(T serviceInstance, IMemoryCache memoryCache)
        {
            object service = Create<T, CacheProxy<T>>();

            ((CacheProxy<T>)service).SetParameters(serviceInstance, memoryCache);

            return (T)service;
        }
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var cacheKey = new
            {
                Decorated = typeof(T).Name,
                MethodName = targetMethod.Name,
                Parameters = args
            };

            var cacheKeyJson = JsonConvert.SerializeObject(cacheKey);

            if(!_memoryCache.TryGetValue(cacheKeyJson, out object result))
            {
                result = targetMethod.Invoke(_service, args);
                _memoryCache.Set(cacheKeyJson, result, TimeSpan.FromMinutes(30));

            }
            return result;
        }
    }
}
