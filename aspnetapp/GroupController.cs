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
	public string? GroupFingerprint { get; set; }
	public string? MemberFingerprint { get; set; }
	public string? LocalAddress { get; set; }
	public ushort Port { get; set; }
}

public class SetPeerResponse
{
	public string? Motd { get; set; }
	public string? ServerName { get; set; }
	public int OnlineUsers { get; set; }
}

public class GetPeerRequest
{
	public string? GroupFingerprint { get; set; }
	public string? MemberFingerprint { get; set; }
	public string? Address { get; set; }
	public string? LocalAddress { get; set; }
	public ushort Port { get; set; }
	public HashSet<string>? Members { get; set; }
}

[Route("[controller]/[action]")]
public class PeerController(IGroupService groupService)
	: Controller
{
	[HttpPost]
	public IActionResult Set([FromBody] SetPeerRequest setRequest)
	{
		string? groupFingerprint = setRequest.GroupFingerprint;
		string? memberFingerprint = setRequest.MemberFingerprint;
		IPAddress? ip = this.HttpContext.Connection.RemoteIpAddress;
		IPAddress? localIp = null;
		IPAddress.TryParse(setRequest.LocalAddress, out localIp);
		ushort port = setRequest.Port;

		// On the digital ocean app platform, remote IP is captured in
		// the do-connecting-ip header by the load balancers.
		this.Request.Headers.TryGetValue("do-connecting-ip", out StringValues digitalOceanClientIp);
		foreach (string? doIp in digitalOceanClientIp)
		{
			if (doIp == null)
				continue;

			ip = IPAddress.Parse(doIp);
		}

		if (string.IsNullOrEmpty(memberFingerprint) || ip == null || port == 0)
			return this.BadRequest();

		SetPeerResponse response = new();
		response.OnlineUsers = groupService.SetMember(groupFingerprint, memberFingerprint, ip, localIp, port);
		response.ServerName = Environment.GetEnvironmentVariable("SERVER_NAME");
		response.Motd = Environment.GetEnvironmentVariable("SERVER_MOTD");
		return Json(response);
	}

	[HttpPost]
	public IActionResult Get([FromBody] GetPeerRequest request)
	{
		if (string.IsNullOrEmpty(request.MemberFingerprint))
			return this.NotFound();

		GetPeerRequest response = request;
		bool valid = groupService.GetMember(
			request.GroupFingerprint,
			request.MemberFingerprint,
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

	[HttpPost]
	public IActionResult GetMembers([FromBody] GetPeerRequest request)
	{
		if (string.IsNullOrEmpty(request.GroupFingerprint))
			return this.NotFound();

		GetPeerRequest response = request;
		response.Members = groupService.GetMembers(request.GroupFingerprint);

		JsonSerializerOptions op = new();
		op.WriteIndented = true;
		string json = JsonSerializer.Serialize(response, op);
		return this.Content(json);
	}
}