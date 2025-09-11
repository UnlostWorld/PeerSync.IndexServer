// .______ _____ ___________   _______   ___   _ _____
//  | ___ \  ___|  ___| ___ \ /  ___\ \ / / \ | /  __ \
//  | |_/ / |__ | |__ | |_/ / \ `--. \ V /|  \| | /  \/
//  |  __/|  __||  __||    /   `--. \ \ / | . ` | |
//  | |   | |___| |___| |\ \  /\__/ / | | | |\  | \__/
//  \_|   \____/\____/\_| \_| \____/  \_/ \_| \_/\____/
//  This software is licensed under the GNU AFFERO GENERAL PUBLIC LICENSE v3

namespace PeerSync;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public class SetPeerRequest
{
	public string? Fingerprint { get; set; }
	public string? LocalAddress { get; set; }
	public ushort Port { get; set; }
}

public class GetPeerRequest
{
	public string? Fingerprint { get; set; }
	public string? Address { get; set; }
	public string? LocalAddress { get; set; }
	public ushort Port { get; set; }
}

[Route("[controller]/[action]")]
public class PeerController(IPeerService syncService)
	: Controller
{
	[HttpPost]
	public IActionResult Set([FromBody] SetPeerRequest heartbeat)
	{
		string? fingerprint = heartbeat.Fingerprint;
		IPAddress? ip = this.HttpContext.Connection.RemoteIpAddress;
		IPAddress? localIp = null;
		IPAddress.TryParse(heartbeat.LocalAddress, out localIp);
		ushort port = heartbeat.Port;

		if (string.IsNullOrEmpty(fingerprint) || ip == null || port == 0)
			return this.BadRequest();

		int peerCount = syncService.SetPeer(fingerprint, ip, localIp, port);
		return this.Content(peerCount.ToString());
	}

	[HttpPost]
	public IActionResult Get([FromBody] GetPeerRequest request)
	{
		string? fingerprint = request.Fingerprint;
		if (string.IsNullOrEmpty(fingerprint))
			return this.NotFound();

		GetPeerRequest response = request;
		bool valid = syncService.GetPeer(
			fingerprint,
			out var address,
			out var localAddress,
			out var port);
		if (valid)
		{
			response.Address = address?.ToString();
			response.LocalAddress = localAddress?.ToString();
			response.Port = port;
		}

		JsonSerializerOptions op = new();
		op.WriteIndented = true;
		string json = JsonSerializer.Serialize(response, op);
		return this.Content(json);
	}
}