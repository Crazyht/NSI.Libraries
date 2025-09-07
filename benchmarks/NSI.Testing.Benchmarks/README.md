# NSI.Testing Benchmarks

Ce projet contient des benchmarks de performance complets pour le système MockLogger, utilisant BenchmarkDotNet pour des mesures précises et reproductibles.

## 🎯 Objectifs des Benchmarks

Les benchmarks couvrent tous les aspects critiques du système MockLogger :

- **Performance de logging** - Mesure des opérations de base et complexes
- **Gestion des scopes** - Performance de création, imbrication et disposal
- **Requêtes LINQ** - Performance des extensions de requête sur de gros datasets
- **Filtrage** - Impact des configurations de filtrage sur les performances
- **Allocation mémoire** - Patterns d'allocation et impact GC
- **Comparaisons** - MockLogger vs logging standard .NET

## 🏗️ Structure du Projet

```
NSI.Testing.Benchmarks/
├── Benchmarks/
│   ├── LoggingPerformanceBenchmarks.cs     # Benchmarks de logging de base
│   ├── ScopePerformanceBenchmarks.cs       # Performance des scopes
│   ├── QueryPerformanceBenchmarks.cs       # Performance des requêtes LINQ
│   ├── FilteringPerformanceBenchmarks.cs   # Impact du filtrage
│   ├── MemoryPerformanceBenchmarks.cs      # Allocation mémoire et GC
│   └── ComparisonBenchmarks.cs             # Comparaisons avec le logging standard
├── Program.cs                              # Point d'entrée
├── NSI.Testing.Benchmarks.csproj          # Configuration du projet
├── .editorconfig                          # Configuration de formatage
└── README.md                              # Ce fichier
```

## 🚀 Exécution des Benchmarks

### Prérequis

- .NET 8.0 SDK ou supérieur
- Projet NSI.Testing compilé
- Permissions d'administrateur (recommandé pour des mesures précises)

### Commandes d'Exécution

```bash
# Compiler le projet
dotnet build -c Release

# Exécuter tous les benchmarks
dotnet run -c Release

# Exécuter des benchmarks spécifiques
dotnet run -c Release -- --filter "*LoggingPerformance*"
dotnet run -c Release -- --filter "*ScopePerformance*"
dotnet run -c Release -- --filter "*QueryPerformance*"
dotnet run -c Release -- --filter "*FilteringPerformance*"
dotnet run -c Release -- --filter "*MemoryPerformance*"
dotnet run -c Release -- --filter "*Comparison*"

# Exécuter avec options spécifiques
dotnet run -c Release -- --filter "*" --job Short --memory
```

### Options BenchmarkDotNet Utiles

```bash
# Tests rapides pour développement
dotnet run -c Release -- --job Dry

# Tests courts avec moins d'itérations
dotnet run -c Release -- --job Short

# Tests avec analyse mémoire détaillée
dotnet run -c Release -- --memory

# Export des résultats
dotnet run -c Release -- --exporters html,json,markdown

# Aide complète
dotnet run -c Release -- --help
```

## 📊 Types de Benchmarks

### 1. LoggingPerformanceBenchmarks

Mesure les performances des opérations de logging de base :

- **MockLogger_BasicLogging** - Logging simple (baseline)
- **MockLogger_StructuredLogging** - Logging structuré avec objets complexes
- **MockLogger_WithExceptions** - Logging avec exceptions
- **MockLogger_StringInterpolation** - Impact de l'interpolation de chaînes
- **MockLogger_HighFrequency** - Logging haute fréquence
- **MockLogger_ComplexState** - États complexes avec dictionnaires
- **ConsoleLogger_Baseline** - Comparaison avec le logging console

**Paramètres :**

- `LogCount`: 100, 1000, 10000 entrées
- `Level`: Debug, Information, Error

### 2. ScopePerformanceBenchmarks

Analyse les performances de gestion des scopes :

- **BasicScopes_CreateAndDispose** - Création/destruction simple (baseline)
- **StructuredScopes_WithVariables** - Scopes avec variables structurées
- **NestedScopes_HierarchyManagement** - Hiérarchies de scopes imbriqués
- **ScopesWithLogging_CombinedActivity** - Scopes avec logging actif
- **ComplexNestedScopes_WithLogging** - Scénarios complexes multiniveaux
- **ScopeDisposal_Timing** - Performance du disposal
- **LargeScopeState_MemoryImpact** - Impact mémoire des gros états
- **ConcurrentScopes_ThreadSafety** - Thread safety sous charge

**Paramètres :**

- `ScopeCount`: 100, 1000, 5000 scopes
- `NestingDepth`: 1, 10, 50 niveaux

### 3. QueryPerformanceBenchmarks

Évalue les performances des requêtes LINQ :

- **Query_BasicTypeFiltering** - Filtrage par type (baseline)
- **Query_LogLevelFiltering** - Filtrage par niveau de log
- **Query_MessageFiltering** - Filtrage par contenu de message
- **Query_RegexMatching** - Correspondance par expressions régulières
- **Query_ScopeFiltering** - Filtrage par variables de scope
- **Query_ComplexGrouping** - Opérations de groupage complexes
- **Query_ScopeHierarchyAnalysis** - Analyse hiérarchique
- **Query_ExceptionAnalysis** - Analyse des exceptions
- **Query_ComplexAnalysis** - Analyses multi-étapes
- **Query_FilteringChains** - Chaînes de filtres
- **Query_LargeResultSetProcessing** - Traitement de gros résultats
- **Query_AggregationOperations** - Opérations d'agrégation

**Paramètres :**

- `DatasetSize`: 1000, 10000, 50000 entrées

### 4. FilteringPerformanceBenchmarks

Mesure l'impact des configurations de filtrage :

- **Unfiltered_AllLevels** - Sans filtrage (baseline)
- **Filtered_InformationAndAbove** - Filtrage modéré
- **HeavyFiltered_WarningsAndErrors** - Filtrage lourd
- **CategoryFiltering_MixedCategories** - Filtrage par catégorie
- **IsEnabledChecks_PerformanceImpact** - Impact des vérifications IsEnabled
- **ScopeFiltering_Performance** - Performance du filtrage de scopes
- **ComplexStateFiltering_Performance** - Filtrage avec états complexes
- **ConcurrentFiltering_ThreadSafety** - Filtrage concurrent
- **RealWorldFiltering_ApplicationScenario** - Scénario d'application réaliste

**Paramètres :**

- `OperationCount`: 1000, 10000 opérations

### 5. MemoryPerformanceBenchmarks

Analyse les patterns d'allocation mémoire :

- **Memory_BasicLogging** - Allocation de base (baseline)
- **Memory_StringInterpolation** - Impact de l'interpolation
- **Memory_StructuredLogging** - Logging structuré
- **Memory_LargeStateObjects** - Gros objets d'état
- **Memory_ScopeAllocation** - Allocation des scopes
- **Memory_NestedScopes** - Scopes imbriqués
- **Memory_ExceptionLogging** - Logging avec exceptions
- **Memory_ArrayState** - États sous forme de tableaux
- **Memory_DictionaryState** - États sous forme de dictionnaires
- **Memory_FormatterFunctions** - Impact des fonctions de formatage
- **Memory_ConcurrentAllocation** - Allocation concurrente
- **Memory_HighLoadPressure** - Pression mémoire élevée
- **Memory_StoreGrowthPatterns** - Patterns de croissance du store
- **Memory_GarbageCollectionImpact** - Impact du garbage collection

**Paramètres :**

- `OperationCount`: 1000, 10000 opérations
- `StateObjectSize`: 10, 100, 1000 propriétés

### 6. ComparisonBenchmarks

Compare MockLogger avec les implémentations standard :

- **NullLogger_Baseline** - Logger null (baseline)
- **MockLogger_Performance** vs **ConsoleLogger_Performance**
- **MockLogger_ScopeComparison** vs **ConsoleLogger_ScopeComparison**
- **MockLogger_StructuredComparison** vs **ConsoleLogger_StructuredComparison**
- **MockLogger_ExceptionComparison** vs **ConsoleLogger_ExceptionComparison**
- **MockFactory_LoggerCreation** vs **ConsoleFactory_LoggerCreation**
- **MockLogger_IsEnabledComparison** vs **ConsoleLogger_IsEnabledComparison**
- **MockLogger_ConcurrentComparison** vs **ConsoleLogger_ConcurrentComparison**
- **MockLogger_RealWorldScenario** vs **ConsoleLogger_RealWorldScenario**

**Paramètres :**

- `OperationCount`: 1000, 10000 opérations

## 📈 Analyse des Résultats

### Métriques Importantes

1. **Throughput** - Opérations par seconde
2. **Mean Time** - Temps moyen d'exécution
3. **Memory Allocation** - Allocation mémoire par opération
4. **Gen 0/1/2 Collections** - Collections garbage collector

### Objectifs de Performance

| Scénario               | Objectif                | Seuil d'Alerte   |
| ---------------------- | ----------------------- | ---------------- |
| Logging de base        | > 100,000 ops/sec       | < 50,000 ops/sec |
| Scopes simples         | > 50,000 ops/sec        | < 25,000 ops/sec |
| Requêtes LINQ          | < 500ms sur 50k entrées | > 1000ms         |
| Allocation mémoire     | < 1KB par entrée        | > 2KB par entrée |
| Overhead vs NullLogger | < 10x                   | > 20x            |

### Interprétation des Résultats

```
BenchmarkDotNet=v0.13.12, OS=Windows 10.0.19045
Intel Core i7-9700K CPU 3.60GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK=8.0.100

|                    Method | OperationCount |      Mean |    Error |   StdDev |    Median |  Gen 0 | Allocated |
|-------------------------- |--------------- |----------:|---------:|---------:|----------:|-------:|----------:|
| MockLogger_BasicLogging   |           1000 |  1.234 ms | 0.012 ms | 0.011 ms |  1.230 ms | 150.0  |    1.2 MB |
```

**Légende :**

- **Mean** : Temps moyen d'exécution
- **Error** : Marge d'erreur statistique
- **StdDev** : Écart-type
- **Gen 0** : Collections garbage collector génération 0
- **Allocated** : Mémoire allouée totale

## 🔧 Configuration et Optimisation

### Variables d'Environnement

```bash
# Optimisations pour benchmarks précis
set DOTNET_TieredCompilation=0
set DOTNET_ReadyToRun=0
set DOTNET_TC_QuickJitForLoops=1

# Collecte de métriques détaillées
set DOTNET_PerfMapEnabled=1
```

### Configuration BenchmarkDotNet

Le projet utilise une configuration personnalisée pour des mesures optimales :

```csharp
var config = DefaultConfig.Instance
  .AddJob(Job.Default
    .WithWarmupCount(3)      # 3 itérations de warm-up
    .WithIterationCount(10)  # 10 itérations de mesure
    .WithInvocationCount(1)  # 1 invocation par itération
    .WithUnrollFactor(1))    # Pas de loop unrolling
  .AddDiagnoser(MemoryDiagnoser.Default)    # Métriques mémoire
  .AddDiagnoser(ThreadingDiagnoser.Default) # Métriques threading
  .AddExporter(MarkdownExporter.GitHub)     # Export Markdown
  .AddExporter(HtmlExporter.Default)        # Export HTML
  .AddExporter(JsonExporter.Full);          # Export JSON
```

## 📝 Bonnes Pratiques

### Avant d'Exécuter les Benchmarks

1. **Fermer les applications** inutiles
2. **Débrancher le laptop** (utiliser l'alimentation secteur)
3. **Désactiver l'antivirus** temporairement
4. **Exécuter en mode Release** uniquement
5. **Utiliser des paramètres cohérents** entre les runs

### Interprétation des Résultats

1. **Comparer les ratios** plutôt que les valeurs absolues
2. **Regarder la médiane** en plus de la moyenne
3. **Analyser les allocations mémoire** autant que le temps
4. **Vérifier la stabilité** (faible écart-type)
5. **Tester sur plusieurs machines** pour validation

### Résolution des Problèmes

#### Résultats Instables

- Augmenter le nombre d'itérations : `--iterationCount 15`
- Augmenter le warm-up : `--warmupCount 5`
- Vérifier la charge système

#### Erreurs OutOfMemory

- Réduire les paramètres `DatasetSize` ou `OperationCount`
- Augmenter la mémoire disponible
- Exécuter les benchmarks séparément

#### Performance Dégradée

- Vérifier la compilation en Release
- Désactiver le debugging
- Analyser les allocations mémoire

## 🎯 Utilisation en CI/CD

### GitHub Actions Example

```yaml
- name: Run Performance Benchmarks
  run: |
    cd benchmarks/NSI.Testing.Benchmarks
    dotnet run -c Release -- --filter "*Comparison*" --job Short --exporters json

- name: Compare Results
  run: |
    # Compare avec les résultats de référence
    dotnet run --project tools/BenchmarkComparer -- results.json baseline.json
```

### Alertes de Régression

```yaml
- name: Check Performance Regression
  run: |
    if [ $(jq '.Results[0].Statistics.Mean' results.json) > 1000 ]; then
      echo "Performance regression detected!"
      exit 1
    fi
```

## 📚 Ressources Supplémentaires

- [Documentation BenchmarkDotNet](https://benchmarkdotnet.org/)
- [Guide des bonnes pratiques](https://benchmarkdotnet.org/articles/guides/good-practices.html)
- [Analyseurs de performance .NET](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/)

## 🤝 Contribution

Pour ajouter de nouveaux benchmarks :

1. Créer une nouvelle classe dans `Benchmarks/`
2. Hériter des attributs appropriés (`[MemoryDiagnoser]`, `[SimpleJob]`)
3. Ajouter des méthodes `[Benchmark]` avec des noms descriptifs
4. Utiliser `[Params]` pour tester différents paramètres
5. Documenter les objectifs et seuils de performance
6. Mettre à jour ce README

### Template de Benchmark

```csharp
[MemoryDiagnoser]
[SimpleJob]
public class MyBenchmarks {
  [Params(100, 1000)]
  public int Size { get; set; }

  [GlobalSetup]
  public void Setup() {
    // Initialisation
  }

  [Benchmark(Baseline = true)]
  public int Baseline() {
    // Implémentation baseline
    return Size;
  }

  [Benchmark]
  public int Optimized() {
    // Implémentation optimisée
    return Size;
  }
}
```

---

**Happy Benchmarking!** 🚀
