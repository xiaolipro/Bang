﻿using BenchmarkDotNet.Attributes;
using Fast.Core.Collections;
using Fast.Core.Helpers;

namespace Fast.Core.Benchmarks.Collections;

[MemoryDiagnoser]
public class SortByDependenciesBenchmark
{
    private readonly Dictionary<char, char[]> _dependencies = new Dictionary<char, char[]>
    {
        {'A', new[] {'B', 'G'}},
        {'B', new[] {'C', 'E'}},
        {'C', new[] {'D'}},
        {'D', Array.Empty<char>()},
        {'E', new[] {'C', 'F'}},
        {'F', new[] {'C'}},
        {'G', new[] {'F'}}
    };
    
    [Benchmark]
    public void A不剪枝()
    {
        var list = RandomHelper.Shuffle(new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' });

        list = list.SortByDependencies(c => _dependencies[c]);

        foreach (var dependency in _dependencies)
        {
            foreach (var dependedValue in dependency.Value)
            {
                if (list.IndexOf(dependency.Key) <= list.IndexOf(dependedValue)) throw new Exception("");
            }
        }
    }
    
    [Benchmark]
    public void B剪枝()
    {
        var list = RandomHelper.Shuffle(new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' });

        list = SortByDependencies2(list, c => _dependencies[c]);

        foreach (var dependency in _dependencies)
        {
            foreach (var dependedValue in dependency.Value)
            {
                if (list.IndexOf(dependency.Key) <= list.IndexOf(dependedValue)) throw new Exception("");
            }
        }
    }

    
    public static List<T> SortByDependencies<T>(
        IEnumerable<T> source,
        Func<T, IEnumerable<T>> getDependencies,
        IEqualityComparer<T> comparer = null)
    {
        /* See: http://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
         *      http://en.wikipedia.org/wiki/Topological_sorting
         */

        var sorted = new List<T>();

        // 维护一个访问记录hash
        var visited = new Dictionary<T, bool>(comparer);

        foreach (var item in source)
        {
            SortByDependenciesVisit(item, getDependencies, sorted, visited);
        }

        return sorted;
    }

    private static void SortByDependenciesVisit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted,
        Dictionary<T, bool> visited)
    {
        var alreadyVisited = visited.TryGetValue(item, out var processing);

        // 如果已经访问过了，并且还在递归栈中，则出现了循环引用
        if (alreadyVisited && processing)
        {
            throw new ArgumentException("Cyclic dependency found! Item: " + item);
        }
        
        visited[item] = true;

        // 递归处理以item为起点，连通的其它点
        var dependencies = getDependencies(item);
        if (dependencies != null)
        {
            foreach (var dependency in dependencies)
            {
                SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
            }
        }
        
        // 此时item的出度为0
        visited[item] = false;
        sorted.Add(item);
    }
    
    
    
    public static List<T> SortByDependencies2<T>(
        IEnumerable<T> source,
        Func<T, IEnumerable<T>> getDependencies,
        IEqualityComparer<T> comparer = null)
    {
        /* See: http://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
         *      http://en.wikipedia.org/wiki/Topological_sorting
         */

        var sorted = new List<T>();

        // 维护一个访问记录hash
        var visited = new Dictionary<T, bool>(comparer);

        foreach (var item in source)
        {
            SortByDependenciesVisit2(item, getDependencies, sorted, visited);
        }

        return sorted;
    }

    private static void SortByDependenciesVisit2<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted,
        Dictionary<T, bool> visited)
    {
        var alreadyVisited = visited.TryGetValue(item, out var processing);

        // 如果已经访问过了，并且还在递归栈中，则出现了循环引用
        if (alreadyVisited && processing)
        {
            throw new ArgumentException("Cyclic dependency found! Item: " + item);
        }
        
        if (alreadyVisited) return;

        visited[item] = true;

        // 递归处理以item为起点，连通的其它点
        var dependencies = getDependencies(item);
        if (dependencies != null)
        {
            foreach (var dependency in dependencies)
            {
                SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
            }
        }
        
        // 此时item的出度为0
        visited[item] = false;
        sorted.Add(item);
    }
}