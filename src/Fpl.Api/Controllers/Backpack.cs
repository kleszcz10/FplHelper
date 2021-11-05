using System.Collections.Generic;
using System.Linq;

namespace Fpl.Api.Controllers
{
    public static class Backpack
    {
        public static IEnumerable<IEnumerable<T>> Combine<T>(IEnumerable<T> source, int count)
        {
            foreach (var team in GetPermutations(source, count))
            {
                yield return team;
            }

            //foreach(var combination in Combinations(count, source.Count()))
            //{
            //    yield return combination.Select(x => source.ElementAt(x - 1));
            //}
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> items, int count)
        {
            int i = 0;
            foreach (var item in items)
            {
                if (count == 1)
                    yield return new T[] { item };
                else
                {
                    foreach (var result in GetPermutations(items.Skip(i + 1), count - 1))
                        yield return new T[] { item }.Concat(result);
                }

                ++i;
            }
        }
    }
}
