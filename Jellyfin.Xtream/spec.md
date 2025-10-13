# CandyTv (Jellyfin.Xtream) - Technical Specification

## Project Overview

**CandyTv** (formerly Jellyfin.Xtream) is a comprehensive Jellyfin plugin that integrates Xtream-compatible API content into the Jellyfin media server. It enables streaming of Live IPTV channels with EPG support, Video On-Demand content, TV Series, and TV catch-up functionality.

### Project Metadata

- **Project Name**: CandyTv
- **Assembly Name**: CandyTv
- **Plugin GUID**: `5d774c35-8567-46d3-a950-9bb8227a0c5d`
- **Version**: 0.0.2
- **License**: GPL-3.0
- **Target Framework**: .NET 8.0
- **Jellyfin Target ABI**: 10.10.7.0
- **Category**: Live TV
- **Repository**: https://github.com/Kevinjil/Jellyfin.Xtream

---

## Architecture Overview

### System Design

The plugin follows a layered architecture pattern:

```
┌─────────────────────────────────────────────────────┐
│           Jellyfin Server Integration               │
│  (ILiveTvService, IChannel, ISupportsDirectStream) │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Channel Layer                          │
│  LiveTvService | VodChannel | SeriesChannel |      │
│  CatchupChannel                                     │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Service Layer                          │
│  StreamService | TaskService | CacheService |       │
│  ConnectionManager | ChannelListService |           │
│  ThumbnailCacheService                              │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Client Layer                           │
│  XtreamClient | ConnectionInfo | JSON Converters    │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│         Xtream-Compatible API Server                │
└─────────────────────────────────────────────────────┘
```

---

## Core Components

### 1. Plugin Entry Point

**File**: `Plugin.cs`

The main plugin class that inherits from `BasePlugin<PluginConfiguration>` and implements `IHasWebPages`.

**Key Responsibilities**:
- Plugin lifecycle management
- Configuration management
- Service initialization (StreamService, TaskService)
- Web UI page registration
- Auto-refresh of TV Guide and Channels on configuration changes

**Important Properties**:
- `Name`: "CandyTv"
- `Id`: `5d774c35-8567-46d3-a950-9bb8227a0c5d`
- `Creds`: Connection credentials from configuration
- `DataVersion`: Used for cache invalidation on updates

**Key Methods**:
- `UpdateConfiguration()`: Triggers automatic refresh of TV guide and channels
- `GetPages()`: Returns embedded web resources for admin UI

---

### 2. Channel Implementations

#### 2.1 LiveTvService

**File**: `LiveTvService.cs`

**Purpose**: Implements `ILiveTvService` and `ISupportsDirectStreamProvider` for live IPTV streaming.

**Key Features**:
- Live TV channel enumeration
- EPG (Electronic Program Guide) data retrieval with 10-minute memory caching
- Direct stream provider support with buffering via Restream
- Channel metadata (number, name, image, tags)

**Key Methods**:
- `GetChannelsAsync()`: Returns all configured live channels
- `GetProgramsAsync()`: Returns EPG programs for a date range (with 10-min cache)
- `GetChannelStreamWithDirectStreamProvider()`: Provides direct stream with Restream buffering

**Data Flow**:
```
Configuration → StreamService → XtreamClient → Xtream API
                                            ↓
                                   Memory Cache (10min)
                                            ↓
                              Jellyfin LiveTV System
```

#### 2.2 VodChannel

**File**: `VodChannel.cs`

**Purpose**: Implements `IChannel` for Video On-Demand content.

**Key Features**:
- Category-based organization
- VOD metadata (title, cover art, creation date, tags)
- Stream source info generation
- Provider ID support for metadata providers

**Structure**:
```
Root → Categories → VOD Streams
```

**Key Methods**:
- `GetChannelItems()`: Returns categories or streams based on folder ID
- `IsEnabledFor()`: Checks if VOD is visible per user

#### 2.3 SeriesChannel

**File**: `SeriesChannel.cs`

**Purpose**: Implements `IChannel` for TV Series content.

**Key Features**:
- Hierarchical structure: Categories → Series → Seasons → Episodes
- Metadata: ratings, genres, cast, air dates, overviews
- Episode video/audio codec information
- Season and episode cover art

**Structure**:
```
Root → Series Categories → Series → Seasons → Episodes
```

**GUID Prefixes**:
- `SeriesCategoryPrefix`: 0x5d774c37
- `SeriesPrefix`: 0x5d774c38
- `SeasonPrefix`: 0x5d774c39
- `EpisodePrefix`: 0x5d774c3a

#### 2.4 CatchupChannel

**File**: `CatchupChannel.cs`

**Purpose**: Implements `IChannel` for TV catch-up (time-shifted viewing).

**Key Features**:
- EPG-based catch-up with configurable archive duration
- Day-by-day browsing interface
- Fallback to full-day catch-up if no EPG data available
- Automatic date-based cache invalidation

**Structure**:
```
Root → Channels (with catch-up) → Days → EPG Programs
```

**Data Version**: Includes current date to force daily refresh

---

### 3. Service Layer

#### 3.1 StreamService

**File**: `Service/StreamService.cs`

**Purpose**: Central service for managing streams, categories, and media source info.

**GUID Encoding System**:

The plugin uses a unique GUID encoding system to identify different content types and hierarchy levels. Each GUID encodes four 32-bit integers:

| Prefix Constant | Hex Value | Purpose |
|----------------|-----------|---------|
| `VodCategoryPrefix` | 0x5d774c35 | VOD category folders |
| `StreamPrefix` | 0x5d774c36 | Stream items |
| `SeriesCategoryPrefix` | 0x5d774c37 | Series category folders |
| `SeriesPrefix` | 0x5d774c38 | Series folders |
| `SeasonPrefix` | 0x5d774c39 | Season folders |
| `EpisodePrefix` | 0x5d774c3a | Episode items |
| `CatchupPrefix` | 0x5d774c3b | Catch-up channel folders |
| `CatchupStreamPrefix` | 0x5d774c3c | Catch-up stream items |
| `MediaSourcePrefix` | 0x5d774c3d | Media source identification |
| `LiveTvPrefix` | 0x5d774c3e | Live TV channels |
| `EpgPrefix` | 0x5d774c3f | EPG program entries |

**Key Methods**:
- `ToGuid(i0, i1, i2, i3)`: Encodes four integers into a GUID
- `FromGuid(guid, out i0, out i1, out i2, out i3)`: Decodes GUID back to integers
- `ParseName(name)`: Extracts tags from stream names (e.g., `[HD]`, `|4K|`)
- `GetLiveStreams()`: Returns configured live channels
- `GetLiveStreamsWithOverrides()`: Returns channels with custom ordering applied
- `GetMediaSourceInfo()`: Generates MediaSourceInfo with stream URLs

**Tag Parsing**:
Supports multiple tag formats:
- `[TAG]` - Square brackets
- `|TAG|` - Pipe delimiters
- Unicode Block Elements (U+2580 - U+259F) as separators

**Channel List Override Logic**:
```csharp
if (config.ChannelMappings != null && config.ChannelMappings.Count > 0)
{
    // Use custom channel list ordering
    var sortedMappings = config.ChannelMappings.Values.OrderBy(m => m.Position);
    // Only return streams that are in the custom channel list
    return orderedStreams;
}
// Otherwise return all configured streams
```

#### 3.2 ConnectionManager

**File**: `Service/ConnectionManager.cs`

**Purpose**: Manages connection queuing to Xtream API to enforce single-connection constraint.

**Features**:
- Optional connection queuing (configurable via `EnableConnectionQueue`)
- Request statistics tracking
- Prevents overwhelming provider with concurrent requests

**Statistics**:
- `IsBusy`: Current connection status
- `QueuedRequests`: Number of pending requests
- `TotalRequests`: Total requests processed

#### 3.3 CacheService

**File**: `Service/CacheService.cs`

**Purpose**: Provides extended caching capabilities for API responses.

**Features**:
- Configurable cache retention (via `EnableExtendedCache`)
- Maintenance window support (configurable hours)
- Cache hit/miss rate tracking

**Statistics**:
- `CacheHitRate`: Percentage of cache hits
- `CacheHits`: Total cache hits
- `CacheMisses`: Total cache misses

#### 3.4 ThumbnailCacheService

**File**: `Service/ThumbnailCacheService.cs`

**Purpose**: Caches thumbnail images from Xtream provider to local disk.

**Features**:
- Configurable cache retention (via `ThumbnailCacheRetentionDays`)
- Disk-based caching in plugin data directory
- URL-to-local-path mapping
- Cache statistics (file count, total size)

**Cache Location**: `{plugin_data_path}/thumbnails/`

**Key Methods**:
- `GetCachedUrlAsync()`: Returns cached local URL or original URL
- `CacheHitRate`: Cache effectiveness metric
- `CachedImages`: Number of cached thumbnails
- `CacheRequests`: Total thumbnail requests

#### 3.5 ChannelListService

**File**: `Service/ChannelListService.cs`

**Purpose**: Handles custom channel list parsing and fuzzy matching.

**Features**:
- TXT file parsing for channel lists
- Fuzzy matching using FuzzySharp library
- Top-N match retrieval
- Exact match detection

**Key Methods**:
- `ParseTxtContent()`: Parses newline-delimited channel names
- `GetTopMatches()`: Returns fuzzy matches with scores

#### 3.6 TaskService

**File**: `Service/TaskService.cs`

**Purpose**: Manages Jellyfin scheduled tasks.

**Key Responsibilities**:
- Cancel running tasks
- Queue tasks for execution
- Trigger TV Guide refresh
- Trigger Channel refresh

#### 3.7 Restream

**File**: `Service/Restream.cs`

**Purpose**: Provides direct stream buffering for live TV.

**Features**:
- HTTP stream buffering
- Consumer count tracking
- Wrapped buffer streams (`WrappedBufferStream`, `WrappedBufferReadStream`)

**Key Properties**:
- `TunerHost`: Identifies restream provider
- `ConsumerCount`: Number of active consumers
- `MediaSource`: Associated media source info

---

### 4. Client Layer

#### 4.1 XtreamClient

**File**: `Client/XtreamClient.cs`

**Purpose**: HTTP client for Xtream API communication.

**Key Features**:
- User-Agent header: `Jellyfin.Xtream/{version}`
- JSON deserialization with Newtonsoft.Json
- Optional connection queue integration
- CancellationToken support

**API Methods**:

| Method | Xtream Action | Returns |
|--------|---------------|---------|
| `GetUserAndServerInfoAsync()` | `player_api.php` | User and server info |
| `GetLiveStreamsAsync()` | `get_live_streams` | All live streams |
| `GetLiveStreamsByCategoryAsync()` | `get_live_streams&category_id=` | Live streams in category |
| `GetLiveCategoryAsync()` | `get_live_categories` | Live TV categories |
| `GetVodStreamsByCategoryAsync()` | `get_vod_streams&category_id=` | VOD streams in category |
| `GetVodInfoAsync()` | `get_vod_info&vod_id=` | VOD metadata |
| `GetVodCategoryAsync()` | `get_vod_categories` | VOD categories |
| `GetSeriesByCategoryAsync()` | `get_series&category_id=` | Series in category |
| `GetSeriesStreamsBySeriesAsync()` | `get_series_info&series_id=` | Series seasons/episodes |
| `GetSeriesCategoryAsync()` | `get_series_categories` | Series categories |
| `GetEpgInfoAsync()` | `get_simple_data_table&stream_id=` | EPG listings |

**Connection Queue Logic**:
```csharp
if (config.EnableConnectionQueue)
{
    return await ConnectionManager.ExecuteAsync(apiCall, null, cancellationToken);
}
else
{
    // Direct call without queueing
    return await client.GetStringAsync(uri, cancellationToken);
}
```

#### 4.2 ConnectionInfo

**File**: `Client/ConnectionInfo.cs`

**Purpose**: Encapsulates Xtream API credentials.

**Properties**:
- `BaseUrl`: API endpoint URL (with protocol, without trailing slash)
- `UserName`: Authentication username
- `Password`: Authentication password

#### 4.3 JSON Converters

The plugin includes custom JSON converters to handle quirks in Xtream API responses:

| Converter | Purpose | Location |
|-----------|---------|----------|
| `Base64Converter` | Decodes Base64-encoded strings | `Client/Base64Converter.cs` |
| `SingularToListConverter` | Converts single objects to lists | `Client/SingularToListConverter.cs` |
| `OnlyObjectConverter` | Handles object-only responses | `Client/OnlyObjectConverter.cs` |
| `IntToBoolConverter` | Converts integers (0/1) to booleans | `Client/IntToBoolConverter.cs` |
| `UnixTimestampConverter` | Converts Unix timestamps to DateTime | `Client/UnixTimestampConverter.cs` |

---

### 5. Data Models

#### 5.1 Client Models

**Location**: `Client/Models/`

**Core Models**:

| Model | Purpose | Key Properties |
|-------|---------|----------------|
| `PlayerApi` | User and server info | `UserInfo`, `ServerInfo` |
| `UserInfo` | User account details | `Username`, `Status`, `ExpDate`, `MaxConnections` |
| `ServerInfo` | Server details | `Url`, `Timezone` |
| `Category` | Content category | `CategoryId`, `CategoryName` |
| `StreamInfo` | Live TV or VOD stream | `StreamId`, `Name`, `StreamIcon`, `Num`, `CategoryId`, `TvArchive`, `TvArchiveDuration`, `ContainerExtension` |
| `Series` | TV series metadata | `SeriesId`, `Name`, `Cover`, `Genre`, `Cast`, `Rating5Based`, `CategoryId` |
| `SeriesStreamInfo` | Series with episodes | `Info`, `Seasons`, `Episodes` |
| `SeriesInfo` | Series metadata | `Name`, `Cover`, `Genre`, `Cast`, `CategoryId` |
| `Season` | Season metadata | `SeasonId`, `Name`, `Cover`, `AirDate`, `Overview` |
| `Episode` | Episode metadata | `EpisodeId`, `Title`, `ContainerExtension`, `Added`, `Info` |
| `EpisodeInfo` | Episode details | `Plot`, `MovieImage`, `Video`, `Audio` |
| `VideoInfo` | Video codec info | `CodecName`, `Width`, `Height`, `AspectRatio`, `BitDepth` |
| `AudioInfo` | Audio codec info | `CodecName`, `Bitrate`, `Channels`, `SampleRate` |
| `EpgListings` | EPG data | `Listings` (list of `EpgInfo`) |
| `EpgInfo` | EPG program entry | `Id`, `Title`, `Description`, `Start`, `End` |
| `VodStreamInfo` | VOD with metadata | `Info`, `MovieData` |
| `VodInfo` | VOD metadata | `TmdbId`, `Name`, `Plot`, `Cast`, `Rating` |

#### 5.2 Configuration Models

**Location**: `Configuration/`

| Model | Purpose | File |
|-------|---------|------|
| `PluginConfiguration` | Main plugin config | `PluginConfiguration.cs` |
| `ChannelOverrides` | Channel customizations | `ChannelOverrides.cs` |
| `ChannelList` | Custom channel list | `ChannelList.cs` |
| `ChannelMapping` | Channel list entry | `ChannelMapping.cs` |
| `SerializableDictionary<K,V>` | XML-serializable dictionary | `SerializableDictionary.cs` |

**PluginConfiguration Properties**:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | string | "https://example.com" | Xtream API base URL |
| `Username` | string | "" | API username |
| `Password` | string | "" | API password |
| `IsCatchupVisible` | bool | false | Show catch-up channel |
| `IsSeriesVisible` | bool | false | Show series channel |
| `IsVodVisible` | bool | false | Show VOD channel |
| `IsTmdbVodOverride` | bool | true | Use TMDB for VOD metadata |
| `LiveTv` | Dictionary | {} | Selected live TV categories/channels |
| `Vod` | Dictionary | {} | Selected VOD categories/streams |
| `Series` | Dictionary | {} | Selected series categories/series |
| `ChannelLists` | Collection | [] | Custom channel lists |
| `ChannelMappings` | Dictionary | {} | Channel list mappings |
| `EnableConnectionQueue` | bool | true | Enable connection queuing |
| `EnableExtendedCache` | bool | true | Enable extended caching |
| `MaintenanceStartHour` | int | 3 | Maintenance window start (0-23) |
| `MaintenanceEndHour` | int | 6 | Maintenance window end (0-23) |
| `EnableEpgPreload` | bool | true | Auto-preload EPG data |
| `EnableMetadataUpdate` | bool | true | Auto-update metadata |
| `EnableThumbnailCache` | bool | true | Enable thumbnail caching |
| `ThumbnailCacheRetentionDays` | int | 30 | Thumbnail cache retention |

**ChannelMapping Properties**:
- `StreamId`: Xtream stream ID
- `Position`: Position in custom list (determines channel number)

#### 5.3 API Models

**Location**: `Api/Models/`

| Model | Purpose | File |
|-------|---------|------|
| `CategoryResponse` | Category for admin UI | `CategoryResponse.cs` |
| `ChannelResponse` | Channel for admin UI | `ChannelResponse.cs` |
| `ItemResponse` | Stream/series for admin UI | `ItemResponse.cs` |
| `ChannelMatchResponse` | Fuzzy match result | `ChannelMatchResponse.cs` |
| `MatchChannelRequest` | Match request payload | `MatchChannelRequest.cs` |
| `ParseChannelListRequest` | Parse request payload | `ParseChannelListRequest.cs` |

---

### 6. API Controller

**File**: `Api/XtreamController.cs`

**Purpose**: RESTful API endpoints for the plugin's admin UI.

**Base Route**: `/Xtream`

**Endpoints**:

| Method | Route | Purpose | Authorization |
|--------|-------|---------|---------------|
| GET | `/LiveCategories` | Get all live TV categories | RequiresElevation |
| GET | `/LiveCategories/{categoryId}` | Get live streams in category | RequiresElevation |
| GET | `/VodCategories` | Get all VOD categories | RequiresElevation |
| GET | `/VodCategories/{categoryId}` | Get VOD streams in category | RequiresElevation |
| GET | `/SeriesCategories` | Get all series categories | RequiresElevation |
| GET | `/SeriesCategories/{categoryId}` | Get series in category | RequiresElevation |
| GET | `/LiveTv` | Get all configured TV channels | RequiresElevation |
| POST | `/ChannelLists/Parse` | Parse TXT channel list | RequiresElevation |
| POST | `/ChannelLists/Match` | Fuzzy match channel name | RequiresElevation |
| GET | `/ChannelLists/AllStreams` | Get all available streams | RequiresElevation |
| GET | `/OptimizationStats` | Get optimization statistics | RequiresElevation |
| POST | `/ResetChannelOrder` | Clear channel mappings | RequiresElevation |
| GET | `/UserInfo` | Get Xtream user/server info | RequiresElevation |
| GET | `/ThumbnailCacheStats` | Get thumbnail cache stats | RequiresElevation |
| POST | `/ClearThumbnailCache` | Clear thumbnail cache | RequiresElevation |

**Optimization Stats Response**:
```json
{
  "isBusy": false,
  "queuedRequests": 0,
  "totalRequests": 1234,
  "cacheHitRate": 0.85,
  "cacheHits": 850,
  "cacheMisses": 150,
  "thumbnailCacheHitRate": 0.92,
  "thumbnailCachedImages": 450,
  "thumbnailCacheRequests": 489
}
```

---

### 7. Providers

#### 7.1 XtreamVodProvider

**File**: `Providers/XtreamVodProvider.cs`

**Purpose**: Metadata provider for VOD content integration with Jellyfin's metadata system.

**Provider Name**: "Xtream"

**Responsibilities**:
- Provide VOD stream IDs for metadata matching
- Support Jellyfin's provider system

---

### 8. Web UI Components

**Location**: `Configuration/Web/`

The plugin provides embedded HTML/CSS/JS resources for the admin UI:

| File | Purpose |
|------|---------|
| `XtreamCredentials.html` | Credentials configuration page |
| `XtreamCredentials.js` | Credentials page logic |
| `XtreamLive.html` | Live TV channel selection |
| `XtreamLive.js` | Live TV selection logic |
| `XtreamLiveOverrides.html` | Channel overrides page |
| `XtreamLiveOverrides.js` | Override management logic |
| `XtreamChannelLists.html` | Custom channel list management |
| `XtreamChannelLists.js` | Channel list logic |
| `XtreamVod.html` | VOD category selection |
| `XtreamVod.js` | VOD selection logic |
| `XtreamSeries.html` | Series category selection |
| `XtreamSeries.js` | Series selection logic |
| `Xtream.css` | Shared CSS styles |
| `Xtream.js` | Shared JavaScript utilities |

**Embedding**:
All web resources are embedded as `EmbeddedResource` in the assembly and served via `Plugin.GetPages()`.

**Resource Path Pattern**:
```
Jellyfin.Xtream.Configuration.Web.{filename}
```

---

## Data Flow Diagrams

### Live TV Channel Flow

```
User Request
    ↓
LiveTvService.GetChannelsAsync()
    ↓
StreamService.GetLiveStreamsWithOverrides()
    ↓
PluginConfiguration (filter by selected categories)
    ↓
ChannelMappings (apply custom ordering if configured)
    ↓
XtreamClient.GetLiveStreamsAsync()
    ↓
ConnectionManager (if enabled)
    ↓
Xtream API: /player_api.php?action=get_live_streams
    ↓
JSON Deserialize (with custom converters)
    ↓
ThumbnailCacheService (cache thumbnails)
    ↓
Return ChannelInfo list to Jellyfin
```

### EPG Data Flow

```
Jellyfin EPG Request
    ↓
LiveTvService.GetProgramsAsync(channelId, startDate, endDate)
    ↓
Check Memory Cache (key: "xtream-epg-{channelId}", TTL: 10min)
    ↓ (cache miss)
XtreamClient.GetEpgInfoAsync(streamId)
    ↓
ConnectionManager (if enabled)
    ↓
Xtream API: /player_api.php?action=get_simple_data_table&stream_id={id}
    ↓
JSON Deserialize
    ↓
Store in Memory Cache (10 minutes)
    ↓
Filter by date range
    ↓
Return ProgramInfo list to Jellyfin
```

### Streaming Flow (Direct Stream)

```
User plays live channel
    ↓
LiveTvService.GetChannelStreamWithDirectStreamProvider()
    ↓
Check existing streams (reuse if available)
    ↓ (new stream needed)
StreamService.GetMediaSourceInfo(StreamType.Live, channelId, restream: true)
    ↓
Generate stream URL: {baseUrl}/{username}/{password}/{streamId}
    ↓
Create Restream instance
    ↓
Restream.Open() - Starts HTTP stream buffering
    ↓
Increment ConsumerCount
    ↓
Return ILiveStream to Jellyfin
    ↓
Jellyfin transcodes/remuxes stream to client
```

### VOD/Series Streaming Flow

```
User plays VOD/episode
    ↓
Channel.GetChannelItems() with media item
    ↓
StreamService.GetMediaSourceInfo()
    ↓
Generate stream URL:
  - VOD: {baseUrl}/movie/{username}/{password}/{streamId}.{ext}
  - Series: {baseUrl}/series/{username}/{password}/{episodeId}.{ext}
    ↓
Return MediaSourceInfo with Path = stream URL
    ↓
Jellyfin directly streams URL (no restream buffering)
```

### Catch-up Streaming Flow

```
User plays catch-up program
    ↓
CatchupChannel.GetChannelItems() with stream items
    ↓
StreamService.GetMediaSourceInfo(StreamType.CatchUp, ...)
    ↓
Generate timeshift URL:
  {baseUrl}/streaming/timeshift.php?
    username={username}&
    password={password}&
    stream={streamId}&
    start={YYYY-MM-DD:HH-mm}&
    duration={minutes}
    ↓
Return MediaSourceInfo with Path = timeshift URL
    ↓
Jellyfin streams time-shifted content
```

---

## Plugin Lifecycle

### 1. Initialization

```
Jellyfin Server Starts
    ↓
PluginServiceRegistrator.RegisterServices()
    ↓
Register singletons:
  - LiveTvService
  - VodChannel
  - SeriesChannel
  - CatchupChannel
  - XtreamVodProvider
  - ConnectionManager
  - CacheService
  - ChannelListService
  - ThumbnailCacheService
    ↓
Plugin constructor called
    ↓
Initialize StreamService and TaskService
    ↓
Plugin.Instance becomes available
```

### 2. Configuration Update

```
User saves configuration in admin UI
    ↓
Plugin.UpdateConfiguration(newConfig)
    ↓
base.UpdateConfiguration(newConfig) - Save to XML
    ↓
TaskService.CancelIfRunningAndQueue(
  "Jellyfin.LiveTv.Guide.RefreshGuideScheduledTask")
    ↓
TaskService.CancelIfRunningAndQueue(
  "Jellyfin.LiveTv.Channels.RefreshChannelsScheduledTask")
    ↓
Jellyfin refreshes all channels and EPG data
```

### 3. Channel Discovery

```
Jellyfin scans for channels
    ↓
LiveTvService.GetChannelsAsync()
    ↓
VodChannel.GetChannelItems(root query)
    ↓
SeriesChannel.GetChannelItems(root query)
    ↓
CatchupChannel.GetChannelItems(root query)
    ↓
Each channel queries Xtream API for configured content
    ↓
Channels are added to Jellyfin's Live TV system
```

---

## Security Considerations

### Known Security Issue: Credential Exposure

**Issue**: Jellyfin publishes remote stream paths in its API and default UI. Since Xtream format includes credentials in URLs, anyone with library access can view credentials.

**URL Format Examples**:
```
Live:    {baseUrl}/{username}/{password}/{streamId}
VOD:     {baseUrl}/movie/{username}/{password}/{streamId}.{ext}
Series:  {baseUrl}/series/{username}/{password}/{episodeId}.{ext}
Catchup: {baseUrl}/streaming/timeshift.php?username={user}&password={pass}&stream={id}&...
```

**Impact**: Users with access to the Jellyfin library can extract API credentials.

**Mitigation**:
- Documented in README.md under "Known problems / Loss of confidentiality"
- Recommend using plugin only on trusted/private servers
- Consider using API credentials with limited permissions if provider supports it

---

## Code Quality and Standards

### Analysis and Enforcement

The project uses strict code quality standards:

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **StyleCop.Analyzers** | C# style enforcement | Version 1.2.0-beta.556 |
| **SerilogAnalyzer** | Logging best practices | Version 0.15.0 |
| **MultithreadingAnalyzer** | Threading safety | Version 1.1.31 |
| **.editorconfig** | Formatting rules | Root of project |
| **jellyfin.ruleset** | Custom analysis rules | Root of project |

### Project Settings

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<Nullable>enable</Nullable>
<AnalysisMode>AllEnabledByDefault</AnalysisMode>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

### Documentation Requirements

All public APIs must have XML documentation comments.

---

## Dependencies

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Jellyfin.Controller** | 10.10.7 | Jellyfin server integration |
| **Jellyfin.Model** | 10.10.7 | Jellyfin data models |
| **Newtonsoft.Json** | 13.0.3 | JSON serialization |
| **FuzzySharp** | 2.0.2 | Fuzzy string matching for channel lists |
| **Microsoft.AspNetCore.App** | (Framework) | ASP.NET Core APIs |

### Analyzer Dependencies (Development)

| Package | Version |
|---------|---------|
| SerilogAnalyzer | 0.15.0 |
| StyleCop.Analyzers | 1.2.0-beta.556 |
| SmartAnalyzers.MultithreadingAnalyzer | 1.1.31 |

---

## Build and Deployment

### Build Commands

```bash
# Debug build
dotnet build Jellyfin.Xtream.sln

# Release build
dotnet build Jellyfin.Xtream.sln -c Release

# Clean
dotnet clean Jellyfin.Xtream.sln

# Restore dependencies
dotnet restore Jellyfin.Xtream.sln
```

### Output

**Assembly**: `CandyTv.dll`

**Location**: `bin/Release/net8.0/CandyTv.dll`

### Installation

1. Copy `CandyTv.dll` to Jellyfin's plugin directory:
   - Windows: `%AppData%\Jellyfin\Server\plugins\CandyTv_0.0.2\`
   - Linux: `/var/lib/jellyfin/plugins/CandyTv_0.0.2/`
   - Docker: `/config/plugins/CandyTv_0.0.2/`

2. Restart Jellyfin server

3. Configure credentials in admin dashboard under Plugins → CandyTv

---

## Configuration Workflow

### 1. Initial Setup (Credentials)

Admin Dashboard → Plugins → CandyTv → Credentials

- Set Base URL (e.g., `https://example.com`)
- Set Username
- Set Password
- Click Save

### 2. Live TV Configuration

**Channel Selection**:

Admin Dashboard → Plugins → CandyTv → Live TV

- Select categories or individual channels
- Enable/disable catch-up visibility
- Click Save

**Channel Overrides**:

Admin Dashboard → Plugins → CandyTv → TV Overrides

- Modify channel numbers
- Rename channels
- Override channel icons
- Click Save

**Custom Channel Lists** (Optional):

Admin Dashboard → Plugins → CandyTv → Channel Lists

- Upload TXT file with channel names (one per line)
- Use fuzzy matching to map to Xtream channels
- Define custom channel ordering
- Click Save

### 3. VOD Configuration

Admin Dashboard → Plugins → CandyTv → Video On-Demand

- Enable "Show this channel to users"
- Select categories or individual videos
- Click Save

### 4. Series Configuration

Admin Dashboard → Plugins → CandyTv → Series

- Enable "Show this channel to users"
- Select categories or individual series
- Click Save

### 5. Advanced Settings

Admin Dashboard → Plugins → CandyTv → Advanced

- Connection queuing settings
- Cache settings (extended cache, EPG preload, metadata updates)
- Thumbnail cache settings
- Maintenance window configuration
- View optimization statistics

---

## Memory and Performance

### Caching Strategy

| Cache Type | Location | TTL | Purpose |
|------------|----------|-----|---------|
| **EPG Cache** | Memory (IMemoryCache) | 10 minutes | Reduce EPG API calls |
| **Extended Cache** | CacheService | Configurable | Reduce general API calls |
| **Thumbnail Cache** | Disk | 30 days (default) | Avoid re-downloading images |

### Connection Management

**Connection Queue**:
- Prevents concurrent API calls if enabled
- Tracks request statistics
- Configurable via `EnableConnectionQueue`

**Benefits**:
- Prevents provider rate limiting
- Reduces server load
- Improves stability with slow providers

### Memory Considerations

**EPG Data**: Cached per-channel for 10 minutes. Large channel lineups with frequent EPG requests may consume significant memory.

**Thumbnails**: Stored on disk, not in memory. Cache size grows based on number of channels and content items.

---

## Troubleshooting

### Common Issues

#### Issue: Channels not appearing

**Causes**:
- Credentials not configured
- Categories not selected
- Network connectivity issues

**Solution**:
1. Verify credentials in admin UI
2. Check category/channel selection
3. Check Jellyfin logs for errors
4. Test API connectivity: `/Xtream/UserInfo`

#### Issue: Streams not playing

**Causes**:
- Invalid stream URLs
- Network issues
- Transcoding problems
- Published server URI not configured

**Solution**:
1. Check Jellyfin networking configuration (`Published server URIs`)
2. Verify stream URL format in logs
3. Test stream URL directly in VLC/browser
4. Check transcoding logs

#### Issue: Missing EPG data

**Causes**:
- Provider does not supply EPG
- EPG cache expired
- API connectivity issues

**Solution**:
1. Force refresh: Admin Dashboard → Live TV → Guide Data → Refresh
2. Check provider EPG availability
3. Review logs for API errors

#### Issue: Slow performance

**Causes**:
- Connection queuing disabled
- Cache disabled
- Large channel/content library
- Slow provider API

**Solution**:
1. Enable connection queue (`EnableConnectionQueue`)
2. Enable extended cache (`EnableExtendedCache`)
3. Enable EPG preload (`EnableEpgPreload`)
4. Reduce selected categories/channels
5. Monitor optimization stats via `/Xtream/OptimizationStats`

---

## API Reference (Xtream)

### Base URL Format

```
https://example.com/player_api.php?username={user}&password={pass}&action={action}
```

### Actions Used

| Action | Parameters | Returns |
|--------|------------|---------|
| `get_live_streams` | `category_id` (optional) | Live TV channels |
| `get_live_categories` | - | Live TV categories |
| `get_vod_streams` | `category_id` | VOD streams |
| `get_vod_info` | `vod_id` | VOD metadata |
| `get_vod_categories` | - | VOD categories |
| `get_series` | `category_id` | Series list |
| `get_series_info` | `series_id` | Series metadata with seasons/episodes |
| `get_series_categories` | - | Series categories |
| `get_simple_data_table` | `stream_id` | EPG data |
| (none) | - | User and server info |

### Stream URL Formats

```
Live:    {baseUrl}/{username}/{password}/{streamId}.{ext}
VOD:     {baseUrl}/movie/{username}/{password}/{streamId}.{ext}
Series:  {baseUrl}/series/{username}/{password}/{episodeId}.{ext}
Catchup: {baseUrl}/streaming/timeshift.php?username={user}&password={pass}&stream={id}&start={YYYY-MM-DD:HH-mm}&duration={minutes}
```

---

## Testing

### Current State

No automated tests are present in the repository.

### Testing Approach

The project relies on:
- GitHub Actions workflows for CI/CD
- CodeQL scanning for security analysis
- Manual testing against live Xtream API endpoints

### Future Improvements

Consider adding:
- Unit tests for service layer
- Integration tests with mock Xtream API
- UI automation tests for admin pages
- Performance tests for large channel lineups

---

## Future Enhancements

### Potential Features

1. **EPG Image Support**: Display EPG program images in catch-up UI
2. **Advanced Filtering**: Search/filter channels by name, tags, or category
3. **Multi-Profile Support**: Different channel configurations per Jellyfin user
4. **Recording Support**: Implement recording functionality via Jellyfin's Live TV system
5. **Statistics Dashboard**: Enhanced admin UI with detailed usage statistics
6. **M3U Export**: Export configured channels as M3U playlist
7. **Provider Testing**: Built-in provider connection testing tool
8. **Backup/Restore**: Configuration backup and restore functionality

### Technical Debt

1. **Testing**: Add comprehensive unit and integration tests
2. **Documentation**: Expand inline code documentation
3. **Error Handling**: Improve error messages for common configuration mistakes
4. **Async/Await**: Review and optimize async patterns throughout codebase
5. **Cancellation**: Ensure all long-running operations respect CancellationToken

---

## Contributing

### Code Style

- Follow StyleCop.Analyzers rules
- Enable nullable reference types
- Document all public APIs with XML comments
- Treat warnings as errors
- Use C# 12 features where appropriate

### Pull Request Process

1. Fork the repository
2. Create feature branch
3. Ensure all analyzers pass
4. Update documentation
5. Submit PR with detailed description

### License

GPL-3.0 - See LICENSE file for details

---

## References

### External Documentation

- [Xtream API Documentation](https://xtream-ui.org/api-xtreamui-xtreamcode/)
- [Jellyfin Plugin Development](https://jellyfin.org/docs/general/server/plugins/)
- [Jellyfin Live TV](https://jellyfin.org/docs/general/server/live-tv/)

### Repository

- **GitHub**: https://github.com/Kevinjil/Jellyfin.Xtream
- **Issues**: https://github.com/Kevinjil/Jellyfin.Xtream/issues

---

## Appendix: File Structure

```
Jellyfin.Xtream/
├── Api/
│   ├── Models/
│   │   ├── ChannelMatchResponse.cs
│   │   ├── ChannelResponse.cs
│   │   ├── CategoryResponse.cs
│   │   ├── ItemResponse.cs
│   │   ├── MatchChannelRequest.cs
│   │   └── ParseChannelListRequest.cs
│   └── XtreamController.cs
├── Client/
│   ├── Models/
│   │   ├── AudioInfo.cs
│   │   ├── Category.cs
│   │   ├── Episode.cs
│   │   ├── EpisodeInfo.cs
│   │   ├── EpgInfo.cs
│   │   ├── EpgListings.cs
│   │   ├── PlayerApi.cs
│   │   ├── Season.cs
│   │   ├── Series.cs
│   │   ├── SeriesInfo.cs
│   │   ├── SeriesStreamInfo.cs
│   │   ├── ServerInfo.cs
│   │   ├── StreamInfo.cs
│   │   ├── UserInfo.cs
│   │   ├── VideoInfo.cs
│   │   ├── VodInfo.cs
│   │   └── VodStreamInfo.cs
│   ├── Base64Converter.cs
│   ├── ConnectionInfo.cs
│   ├── IntToBoolConverter.cs
│   ├── OnlyObjectConverter.cs
│   ├── SingularToListConverter.cs
│   ├── UnixTimestampConverter.cs
│   └── XtreamClient.cs
├── Configuration/
│   ├── Web/
│   │   ├── Xtream.css
│   │   ├── Xtream.js
│   │   ├── XtreamChannelLists.html
│   │   ├── XtreamChannelLists.js
│   │   ├── XtreamCredentials.html
│   │   ├── XtreamCredentials.js
│   │   ├── XtreamLive.html
│   │   ├── XtreamLive.js
│   │   ├── XtreamLiveOverrides.html
│   │   ├── XtreamLiveOverrides.js
│   │   ├── XtreamSeries.html
│   │   ├── XtreamSeries.js
│   │   ├── XtreamVod.html
│   │   └── XtreamVod.js
│   ├── ChannelList.cs
│   ├── ChannelMapping.cs
│   ├── ChannelOverrides.cs
│   ├── PluginConfiguration.cs
│   └── SerializableDictionary.cs
├── Providers/
│   └── XtreamVodProvider.cs
├── Service/
│   ├── CacheService.cs
│   ├── ChannelListService.cs
│   ├── ConnectionManager.cs
│   ├── MatchResult.cs
│   ├── ParsedName.cs
│   ├── Restream.cs
│   ├── StreamService.cs
│   ├── StreamType.cs
│   ├── TaskService.cs
│   ├── ThumbnailCacheService.cs
│   ├── WrappedBufferReadStream.cs
│   └── WrappedBufferStream.cs
├── CatchupChannel.cs
├── LiveTvService.cs
├── Plugin.cs
├── PluginServiceRegistrator.cs
├── SeriesChannel.cs
└── VodChannel.cs
```

---

## Document Version

**Version**: 1.0
**Date**: 2025-01-13
**Based on**: CandyTv v0.0.2 / Jellyfin.Xtream codebase
**Author**: Generated from source code analysis

---

**End of Specification**
