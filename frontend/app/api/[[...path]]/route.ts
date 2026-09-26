import { NextRequest, NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

async function handleProxy(req: NextRequest, { params }: { params: { path?: string[] } }) {
  const backendUrl = process.env.BACKEND_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  const subpath = params.path ? params.path.join('/') : '';
  const search = req.nextUrl.search || '';
  const targetUrl = `${backendUrl.replace(/\/$/, '')}/api/${subpath}${search}`;

  const reqHeaders = new Headers(req.headers);
  // Remove host header so target server receives correct host
  reqHeaders.delete('host');

  const options: RequestInit = {
    method: req.method,
    headers: reqHeaders,
    redirect: 'manual',
  };

  if (req.method !== 'GET' && req.method !== 'HEAD') {
    options.body = await req.arrayBuffer();
  }

  try {
    const res = await fetch(targetUrl, options);
    const resHeaders = new Headers();

    res.headers.forEach((val, key) => {
      const lower = key.toLowerCase();
      if (lower !== 'content-encoding' && lower !== 'content-length' && lower !== 'transfer-encoding') {
        resHeaders.set(key, val);
      }
    });

    // Preserve multiple Set-Cookie headers if present
    if (typeof res.headers.getSetCookie === 'function') {
      const cookies = res.headers.getSetCookie();
      if (cookies.length > 0) {
        resHeaders.delete('set-cookie');
        for (const cookie of cookies) {
          resHeaders.append('set-cookie', cookie);
        }
      }
    }

    return new NextResponse(res.body, {
      status: res.status,
      statusText: res.statusText,
      headers: resHeaders,
    });
  } catch (err: any) {
    return NextResponse.json(
      {
        error: 'Failed to communicate with backend service',
        backendUrl,
        details: err.message,
      },
      { status: 502 }
    );
  }
}

export {
  handleProxy as GET,
  handleProxy as POST,
  handleProxy as PUT,
  handleProxy as PATCH,
  handleProxy as DELETE,
  handleProxy as HEAD,
  handleProxy as OPTIONS,
};
