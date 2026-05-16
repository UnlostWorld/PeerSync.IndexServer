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
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

public interface IGroupService
{
	int Count { get; }

	int SetMember(string? groupFingerprint, string peerFingerprint, IPAddress address, IPAddress? localAddress, ushort port);
	HashSet<string> GetMembers(string? groupFingerprint);
	bool GetMember(string? groupFingerprint, string peerFingerprint, out IPAddress? address, out IPAddress? localAddress, out ushort port);
}

public class GroupService : IGroupService
{
	protected readonly ILogger Log;
	private readonly ConcurrentDictionary<string, Group> groups = new();
	private readonly Timer cleanupTimer;

	public int Count => this.groups.Count;

	private string defaultGroupFingerprint = new Guid().ToString();

	public GroupService(ILogger<GroupService> log)
	{
		this.Log = log;
		this.Log.LogInformation("Group service online");

		this.cleanupTimer = new(this.Clean, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
	}

	public int SetMember(string? groupFingerprint, string memberFingerprint, IPAddress address, IPAddress? localAddress, ushort port)
	{
		if (groupFingerprint == null)
			groupFingerprint = defaultGroupFingerprint;

		if (!this.groups.ContainsKey(groupFingerprint))
			this.groups.TryAdd(groupFingerprint, new Group());

		Group group = this.groups[groupFingerprint];

		if (!group.Peers.ContainsKey(memberFingerprint))
			group.Peers.TryAdd(memberFingerprint, default);

		Peer entry = group.Peers[memberFingerprint];
		entry.Address = address;
		entry.LocalAddress = localAddress;
		entry.Port = port;
		entry.Updated = DateTime.UtcNow;
		group.Peers[memberFingerprint] = entry;

		return group.Peers.Count;
	}

	public HashSet<string> GetMembers(string? groupFingerprint)
	{
		HashSet<string> memberFingerprints = new();

		// Nobody is allowed to get the member list of the public group.
		if (groupFingerprint != null)
		{
			bool success = this.groups.TryGetValue(groupFingerprint, out Group? group);

			if (success && group != null)
			{
				foreach ((string memberFingerprint, Peer peer) in group.Peers)
				{
					memberFingerprints.Add(memberFingerprint);
				}
			}
		}

		// Add dummy fingerprints until we have 1024 entries to return.
		/*while (memberFingerprints.Count < 1024)
		{
			HashAlgorithm algorithm = SHA256.Create();
			string input = $"{DateTime.UtcNow.Millisecond * Random.Shared.NextDouble()}";
			byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
			string dummy = BitConverter.ToString(bytes);
			dummy = dummy.Replace("-", string.Empty, StringComparison.Ordinal);
			memberFingerprints.Add(dummy);
		}*/

		return memberFingerprints;
	}

	public bool GetMember(string? groupFingerprint, string memberFingerprint, out IPAddress? address, out IPAddress? localAddress, out ushort port)
	{
		if (groupFingerprint == null)
			groupFingerprint = defaultGroupFingerprint;

		address = null;
		localAddress = null;
		port = 0;

		bool success = this.groups.TryGetValue(groupFingerprint, out Group? group);
		if (!success || group == null)
			return false;

		success = group.Peers.TryGetValue(memberFingerprint, out Peer peer);
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
		foreach ((string groupFingerprint, Group group) in this.groups)
		{
			group.Clean();

			// If this group has 0 online members, flush it.
			if (group.Peers.Count <= 0)
			{
				offlineFingerprints.Add(groupFingerprint);
			}
		}

		foreach (string fingerprint in offlineFingerprints)
		{
			this.groups.TryRemove(fingerprint, out Group? group);
		}
	}

	public class Group
	{
		public ConcurrentDictionary<string, Peer> Peers = new();

		public void Clean()
		{
			HashSet<string> offlineFingerprints = new();
			foreach ((string fingerprint, Peer peer) in this.Peers)
			{
				TimeSpan age = DateTime.UtcNow - peer.Updated;
				if (age >= TimeSpan.FromSeconds(120))
				{
					offlineFingerprints.Add(fingerprint);
				}
			}

			foreach (string fingerprint in offlineFingerprints)
			{
				this.Peers.TryRemove(fingerprint, out Peer peer);
			}
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