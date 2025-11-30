// .______ _____ ___________   _______   ___   _ _____
//  | ___ \  ___|  ___| ___ \ /  ___\ \ / / \ | /  __ \
//  | |_/ / |__ | |__ | |_/ / \ `--. \ V /|  \| | /  \/
//  |  __/|  __||  __||    /   `--. \ \ / | . ` | |
//  | |   | |___| |___| |\ \  /\__/ / | | | |\  | \__/
//  \_|   \____/\____/\_| \_| \____/  \_/ \_| \_/\____/
//  This software is licensed under the GNU AFFERO GENERAL PUBLIC LICENSE v3

namespace PeerSync;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

public class Program
{
	// TODO: Move this to a configuration pn
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		var services = builder.Services;

		services.AddControllers();
		services.AddHealthChecks();

		services.AddSingleton<IPeerService, PeerService>();

		WebApplication app = builder.Build();
		app.MapHealthChecks("/healthz");
		app.MapControllers();

		app.MapGet("/Environment", () => new EnvironmentInfo());

		app.MapGet("/Index", (IPeerService peerService) =>
		{
			return $"Peer Sync Index Server is online with {peerService.Count} users";
		});

		ForwardedHeadersOptions forwardedHeaders = new();
		forwardedHeaders.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
		forwardedHeaders.KnownProxies.Clear();
		forwardedHeaders.KnownNetworks.Clear();
		app.UseForwardedHeaders(forwardedHeaders);

		app.Run();
	}
}