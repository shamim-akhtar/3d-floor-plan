# 3d-floor-plan

This repo now includes a Unity 6 floor-plan-to-3D workflow inside [`FloorPlanTo3D`](./FloorPlanTo3D).

## What it does

- Loads a local PNG or JPG floor plan image.
- Sends the image to the Google Gemini API for structured extraction of walls, doors, windows, rooms, and footprint data.
- Saves the extracted JSON locally using an image-content hash so the same floor plan does not get sent again.
- Builds a first-pass 3D house in Unity from the extracted JSON.
- Boots a polished runtime UI automatically when you press Play in the sample scene.

## Setup

1. Open `FloorPlanTo3D` in Unity `6000.4.4f1`.
2. Edit [`FloorPlanTo3D/.env`](./FloorPlanTo3D/.env) and fill in `GEMINI_API_KEY`.
3. Press Play in `Assets/Scenes/SampleScene.unity`.
4. Use `Browse` in the editor or paste a full image path into the UI.
5. Click `Generate 3D House`.

## Cache behavior

- Cached JSON files are written to `Application.persistentDataPath/FloorPlanCache`.
- The cache key is a SHA-256 hash of the source image bytes.
- If the image content is unchanged, the app reuses the cached JSON instead of calling Gemini again.

## Current assumptions

- Single-storey interpretation only.
- Best suited to clean top-down raster floor plans.
- Straight wall centerline segments.
- Metric output in meters.
- Dimensions may be inferred when the image does not explicitly label them.

## Main scripts

- [`FloorPlanAppController.cs`](./FloorPlanTo3D/Assets/Scripts/FloorPlan/App/FloorPlanAppController.cs)
- [`GeminiFloorPlanExtractor.cs`](./FloorPlanTo3D/Assets/Scripts/FloorPlan/Services/GeminiFloorPlanExtractor.cs)
- [`HouseGenerator.cs`](./FloorPlanTo3D/Assets/Scripts/FloorPlan/Generation/HouseGenerator.cs)
- [`DotEnvLoader.cs`](./FloorPlanTo3D/Assets/Scripts/FloorPlan/Config/DotEnvLoader.cs)
