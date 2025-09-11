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
using Microsoft.Extensions.Primitives;

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
	public IActionResult Set([FromBody] SetPeerRequest setRequest)
	{
		string? fingerprint = setRequest.Fingerprint;
		IPAddress? ip = this.HttpContext.Connection.RemoteIpAddress;
		IPAddress? localIp = null;
		IPAddress.TryParse(setRequest.LocalAddress, out localIp);
		ushort port = setRequest.Port;

		// On the digital ocean app platform, remote IP is captured in
		// the HTTP_DO_CONNECTING_IP header by the load balancers.
		this.Request.Headers.TryGetValue("HTTP_DO_CONNECTING_IP", out StringValues digitalOceanClientIp);
		foreach (string? doIp in digitalOceanClientIp)
		{
			if (doIp == null)
				continue;

			ip = IPAddress.Parse(doIp);
		}

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