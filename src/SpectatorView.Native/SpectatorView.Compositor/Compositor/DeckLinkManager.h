// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#if defined(INCLUDE_BLACKMAGIC)
#include "IFrameProvider.h"
#include "DeckLinkDevice.h"

class DeckLinkManager : public IFrameProvider
{
public:
    DeckLinkManager(bool useCPU = false, bool passthroughOutput = false);
    ~DeckLinkManager();

    HRESULT Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture);
    ProviderType GetProviderType() { return BlackMagic; }

    // Get the timestamp of the earliest (and currently rendered) cached frame.
    LONGLONG GetTimestamp(int frame);

    LONGLONG GetDurationHNS();

    int GetCaptureFrameIndex();
    int GetPixelChange(int frame);
    int GetNumQueuedOutputFrames();
    void SetLatencyPreference(float latencyPreference);

    void Update(int compositeFrameIndex);

    bool IsEnabled();
    bool SupportsOutput();

    void Dispose();

    bool ProvidesYUV();
    bool ExpectsYUV();

private:
    DeckLinkDeviceDiscovery* deckLinkDiscovery = nullptr;
    DeckLinkDevice* deckLinkDevice = nullptr;
    IDeckLink* deckLink = nullptr;
    bool supportsBlackMagic = true;

    bool _useCPU;
    bool _passthroughOutput;
};
#endif

