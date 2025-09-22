using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;
using NSI.Testing.Tests.TestUtilities;

namespace NSI.Testing.Tests.Unit.Loggers;
/// <summary>
/// Tests for the <see cref="InMemoryLogEntryStore"/> functionality and thread safety.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the correct storage, retrieval, and thread-safe behavior
/// of the in-memory log entry store. They cover both single-threaded and
/// concurrent access scenarios.
/// </para>
/// </remarks>
public class InMemoryLogEntryStoreTests {
  private readonly Lock _Lock = new();

  [Fact]
  public void Add_WithValidEntry_ShouldStoreEntry() {
    // Setup store and test entry
    var store = new InMemoryLogEntryStore();
    var entry = LogEntryFactory.CreateLogEntry(LogLevel.Information, "Test message");

    // Execute add operation
    store.Add(entry);

    // Verify entry is stored
    var entries = store.GetAll();
    Assert.Single(entries);
    Assert.Equal(entry.Message, entries[0].Message);
    Assert.Equal(entry.Level, entries[0].Level);
  }

  [Fact]
  public void Add_WithNullEntry_ShouldThrowArgumentNullException() {
    // Setup store
    var store = new InMemoryLogEntryStore();

    // Execute and verify exception
    var exception = Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    Assert.Equal("entry", exception.ParamName);
  }

  [Fact]
  public void Add_WithMultipleEntries_ShouldMaintainOrder() {
    // Setup store and test entries
    var store = new InMemoryLogEntryStore();
    var entry1 = LogEntryFactory.CreateLogEntry(LogLevel.Debug, "First message");
    var entry2 = LogEntryFactory.CreateLogEntry(LogLevel.Warning, "Second message");
    var entry3 = LogEntryFactory.CreateLogEntry(LogLevel.Error, "Third message");

    // Execute add operations in order
    store.Add(entry1);
    store.Add(entry2);
    store.Add(entry3);

    // Verify order is maintained
    var entries = store.GetAll();
    Assert.Equal(3, entries.Count);
    Assert.Equal("First message", entries[0].Message);
    Assert.Equal("Second message", entries[1].Message);
    Assert.Equal("Third message", entries[2].Message);
  }

  [Fact]
  public void GetAll_WithEmptyStore_ShouldReturnEmptyList() {
    // Setup empty store
    var store = new InMemoryLogEntryStore();

    // Execute get all operation
    var entries = store.GetAll();

    // Verify empty list is returned
    Assert.NotNull(entries);
    Assert.Empty(entries);
  }

  [Fact]
  public void GetAll_ShouldReturnSnapshotNotLiveReference() {
    // Setup store with initial entry
    var store = new InMemoryLogEntryStore();
    var initialEntry = LogEntryFactory.CreateLogEntry(LogLevel.Information, "Initial");
    store.Add(initialEntry);

    // Get first snapshot
    var firstSnapshot = store.GetAll();

    // Add another entry
    var additionalEntry = LogEntryFactory.CreateLogEntry(LogLevel.Warning, "Additional");
    store.Add(additionalEntry);

    // Get second snapshot
    var secondSnapshot = store.GetAll();

    // Verify snapshots are independent
    Assert.Single(firstSnapshot);
    Assert.Equal(2, secondSnapshot.Count);
    Assert.Equal("Initial", firstSnapshot[0].Message);
    Assert.Equal("Additional", secondSnapshot[1].Message);
  }

  [Fact]
  public void Clear_WithEntries_ShouldRemoveAllEntries() {
    // Setup store with multiple entries
    var store = new InMemoryLogEntryStore();
    store.Add(LogEntryFactory.CreateLogEntry(LogLevel.Information, "Entry 1"));
    store.Add(LogEntryFactory.CreateLogEntry(LogLevel.Warning, "Entry 2"));
    store.Add(LogEntryFactory.CreateLogEntry(LogLevel.Error, "Entry 3"));

    // Verify entries exist
    Assert.Equal(3, store.GetAll().Count);

    // Execute clear operation
    store.Clear();

    // Verify all entries are removed
    var entries = store.GetAll();
    Assert.Empty(entries);
  }

  [Fact]
  public void Clear_WithEmptyStore_ShouldNotThrow() {
    // Setup empty store
    var store = new InMemoryLogEntryStore();

    // Execute clear operation (should not throw)
    store.Clear();

    // Verify store remains empty
    Assert.Empty(store.GetAll());
  }

  [Fact]
  public async Task ThreadSafety_ConcurrentAdds_ShouldHandleCorrectly() {
    // Setup store and concurrent operations
    var store = new InMemoryLogEntryStore();
    const int threadCount = 10;
    const int entriesPerThread = 100;
    var tasks = new Task[threadCount];

    // Execute concurrent add operations
    for (var i = 0; i < threadCount; i++) {
      var threadId = i;
      tasks[i] = Task.Run(() => {
        for (var j = 0; j < entriesPerThread; j++) {
          var entry = LogEntryFactory.CreateLogEntry(
            LogLevel.Information,
            $"Thread {threadId} - Entry {j}");
          store.Add(entry);
        }
      });
    }

    // Wait for all tasks to complete
    await Task.WhenAll(tasks);

    // Verify all entries were added
    var entries = store.GetAll();
    Assert.Equal(threadCount * entriesPerThread, entries.Count);

    // Verify no entries are null (indicating thread safety issues)
    Assert.All(entries, entry => Assert.NotNull(entry));
    Assert.All(entries, entry => Assert.NotNull(entry.Message));
  }

  [Fact]
  public async Task ThreadSafety_ConcurrentGetAll_ShouldReturnConsistentSnapshots() {
    // Setup store with initial entries
    var store = new InMemoryLogEntryStore();
    for (var i = 0; i < 50; i++) {
      store.Add(LogEntryFactory.CreateLogEntry(LogLevel.Information, $"Entry {i}"));
    }

    var results = new List<IReadOnlyList<LogEntry>>();
    var tasks = new Task[20];

    // Execute concurrent GetAll operations
    for (var i = 0; i < tasks.Length; i++) {
      tasks[i] = Task.Run(() => {
        var snapshot = store.GetAll();
        lock (results) {
          results.Add(snapshot);
        }
      });
    }

    // Wait for all tasks to complete
    await Task.WhenAll(tasks);

    // Verify all snapshots have consistent data
    Assert.Equal(20, results.Count);
    Assert.All(results, snapshot => Assert.Equal(50, snapshot.Count));

    // Verify all snapshots contain the same data
    var firstSnapshot = results[0];
    foreach (var snapshot in results.Skip(1)) {
      for (var i = 0; i < firstSnapshot.Count; i++) {
        Assert.Equal(firstSnapshot[i].Message, snapshot[i].Message);
      }
    }
  }

  [Fact]
  public async Task ThreadSafety_ConcurrentAddAndGetAll_ShouldMaintainConsistency() {
    // Setup store
    var store = new InMemoryLogEntryStore();
    var addTasks = new Task[5];
    var getTasks = new Task[5];
    var snapshots = new List<IReadOnlyList<LogEntry>>();

    // Start concurrent add operations
    for (var i = 0; i < addTasks.Length; i++) {
      var threadId = i;
      addTasks[i] = Task.Run(() => {
        for (var j = 0; j < 20; j++) {
          store.Add(LogEntryFactory.CreateLogEntry(
            LogLevel.Information,
            $"Thread {threadId} - Entry {j}"));
          Thread.Sleep(1); // Small delay to increase contention
        }
      });
    }

    // Start concurrent get operations
    for (var i = 0; i < getTasks.Length; i++) {
      getTasks[i] = Task.Run(() => {
        for (var j = 0; j < 10; j++) {
          var snapshot = store.GetAll();
          lock (_Lock) {
            snapshots.Add(snapshot);
          }
          Thread.Sleep(2); // Small delay
        }
      });
    }

    // Wait for all operations to complete
    await Task.WhenAll([.. addTasks, .. getTasks]);

    // Verify final state
    var finalEntries = store.GetAll();
    Assert.Equal(100, finalEntries.Count); // 5 threads x 20 entries each

    // Verify snapshots are consistent (no partial reads)
    Assert.All(snapshots, snapshot => {
      Assert.NotNull(snapshot);
      Assert.All(snapshot, entry => {
        Assert.NotNull(entry);
        Assert.NotNull(entry.Message);
      });
    });
  }

  [Fact]
  public async Task ThreadSafety_ConcurrentClearAndAdd_ShouldMaintainConsistency() {
    // Setup store with initial data
    var store = new InMemoryLogEntryStore();
    for (var i = 0; i < 10; i++) {
      store.Add(LogEntryFactory.CreateLogEntry(LogLevel.Information, $"Initial {i}"));
    }

    var tasks = new Task[10];

    // Execute concurrent clear and add operations
    for (var i = 0; i < tasks.Length; i++) {
      var threadId = i;
      tasks[i] = Task.Run(() => {
        if (threadId % 2 == 0) {
          // Clear operations
          store.Clear();
        } else {
          // Add operations
          for (var j = 0; j < 5; j++) {
            store.Add(LogEntryFactory.CreateLogEntry(
              LogLevel.Warning,
              $"Thread {threadId} - Entry {j}"));
          }
        }
      });
    }

    // Wait for all operations to complete
    await Task.WhenAll(tasks);

    // Verify store is in a consistent state (no exceptions thrown)
    var finalEntries = store.GetAll();
    Assert.NotNull(finalEntries);

    // All remaining entries should be valid
    Assert.All(finalEntries, entry => {
      Assert.NotNull(entry);
      Assert.NotNull(entry.Message);
    });
  }

  [Fact]
  public void Performance_LargeNumberOfEntries_ShouldHandleEfficiently() {
    // Setup store and large dataset
    var store = new InMemoryLogEntryStore();
    const int entryCount = 10000;

    // Measure add performance
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    for (var i = 0; i < entryCount; i++) {
      store.Add(LogEntryFactory.CreateLogEntry(
        LogLevel.Information,
        $"Performance test entry {i}"));
    }

    stopwatch.Stop();

    // Verify all entries were added
    var entries = store.GetAll();
    Assert.Equal(entryCount, entries.Count);

    // Performance should be reasonable (< 1 second for 10k entries)
    Assert.True(
      stopwatch.ElapsedMilliseconds < 1000,
      "Adding entries took too long (expected < 1000ms)");
  }
}
