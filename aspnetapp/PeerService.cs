// .______ _____ ___________   _______   ___   _ _____
//  | ___ \  ___|  ___| ___ \ /  ___\ \ / / \ | /  __ \
//  | |_/ / |__ | |__ | |_/ / \ `--. \ V /|  \| | /  \/
//  |  __/|  __||  __||    /   `--. \ \ / | . ` | |
//  | |   | |___| |___| |\ \  /\__/ / | | | |\  | \__/
//  \_|   \____/\____/\_| \_| \____/  \_/ \_| \_/\____/
//  This software is licensed under the GNU AFFERO GENERAL PUBLIC LICENSE v3

namespace PeerSync;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

public interface IPeerService
{
	int Count { get; }
	int SetPeer(string identifier, IPAddress address, IPAddress? localAddress, ushort port);
	bool GetPeer(string identifier, out IPAddress? address, out IPAddress? localAddress, out ushort port);
}

public class PeerService : IPeerService
{
	protected readonly ILogger Log;
	private readonly ConcurrentDictionary<string, Peer> peers = new();
	private readonly Timer cleanupTimer;

	public int Count => this.peers.Count;

	public PeerService(ILogger<PeerService> log)
	{
		this.Log = log;
		this.Log.LogInformation("Sync service online");

		this.cleanupTimer = new(this.Clean, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
	}

	public int SetPeer(string fingerprint, IPAddress address, IPAddress? localAddress, ushort port)
	{
		if (!this.peers.ContainsKey(fingerprint))
			this.peers.TryAdd(fingerprint, default);

		Peer entry = this.peers[fingerprint];
		entry.Address = address;
		entry.LocalAddress = localAddress;
		entry.Port = port;
		entry.Updated = DateTime.UtcNow;
		this.peers[fingerprint] = entry;

		return this.peers.Count;
	}

	public bool GetPeer(string fingerprint, out IPAddress? address, out IPAddress? localAddress, out ushort port)
	{
		bool success = this.peers.TryGetValue(fingerprint, out Peer peer);
		address = peer.Address;
		localAddress = peer.LocalAddress;
		port = peer.Port;

		if (success)
		{
			TimeSpan age = DateTime.UtcNow - peer.Updated;
			if (age >= TimeSpan.FromSeconds(120))
			{
				return false;
			}
		}

		return success;
	}

	private void Clean(object? state)
	{
		HashSet<string> offlineFingerprints = new();
		foreach ((string fingerprint, Peer peer) in this.peers)
		{
			TimeSpan age = DateTime.UtcNow - peer.Updated;
			if (age >= TimeSpan.FromSeconds(120))
			{
				offlineFingerprints.Add(fingerprint);
			}
		}

		foreach (string fingerprint in offlineFingerprints)
		{
			this.peers.TryRemove(fingerprint, out Peer peer);
		}
	}

	public struct Peer
	{
		public IPAddress Address;
		public IPAddress? LocalAddress;
		public ushort Port;
		public DateTime Updated;
	}
}