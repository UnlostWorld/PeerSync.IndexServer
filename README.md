## Deploy using Digital Ocean App Plaform (Recommended)
Use this button to deploy a Digital Ocean App:

[![Deploy to DigitalOcean](https://www.deploytodo.com/do-btn-blue.svg)](https://cloud.digitalocean.com/apps/new?repo=https://github.com/UnlostWorld/PeerSync.IndexServer/tree/main)

#### OR
Use this referral button to support Peer-Sync's default index server and our other server-based projects:

[![Deploy to DigitalOcean](https://www.deploytodo.com/do-btn-white-ghost.svg)](https://cloud.digitalocean.com/apps/new?repo=https://github.com/UnlostWorld/PeerSync.IndexServer/tree/main&refcode=5c8651a096f7)
> Everyone you refer gets $200 in credit over 60 days. Once they’ve spent $25 with us, you'll get $25. There is no limit to the amount of credit you can earn through referrals.


We recommend reducing the app platform container size unless you anticipate very heavy usage:
<img width="600" src="https://github.com/user-attachments/assets/be20e8ed-0dce-4846-896c-ef73fb2594f8" />
<img width="600" src="https://github.com/user-attachments/assets/07ed4d32-f1a8-40c5-810e-b653ee2a06a9" />

⚠️ Please note that the smallest container size comes with 50Gb of monthly outbound bandwidth traffic, and subsequent usage is [billed](https://docs.digitalocean.com/platform/billing/bandwidth/#app-platform). 
We do not currently have anticipated usage estimates, please [check costs](https://www.digitalocean.com/pricing/app-platform) and pay attention to before comitting to running an index server.

Once deployed and live, you can find the URL to your index server at the top of the app page, it should look like `https://your-app-name-1a2b3c.ondigitalocean.app/`
