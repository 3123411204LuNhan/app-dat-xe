using System.Globalization;

namespace RideHailingApp.Services
{
    public static class MapHtmlService
    {
        private const string LeafletCss = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
        private const string LeafletJs  = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";
        private const string OsmTiles   = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";

        // ── Bản đồ chọn điểm đón (toàn màn hình, kéo tâm để chọn) ────────────
        public static string GetPickupMapHtml(double lat, double lon)
        {
            string latStr = lat.ToString(CultureInfo.InvariantCulture);
            string lonStr = lon.ToString(CultureInfo.InvariantCulture);

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""initial-scale=1, width=device-width"">
<link rel=""stylesheet"" href=""{LeafletCss}""/>
<style>
* {{ margin:0; padding:0; box-sizing:border-box; }}
html, body {{ width:100%; height:100%; overflow:hidden; background:#E4DAC8; }}
#map {{ width:100%; height:100%; }}
</style>
</head>
<body>
<div id=""map""></div>
<script src=""{LeafletJs}""></script>
<script>
var map = L.map('map', {{
    center: [{latStr}, {lonStr}],
    zoom: 16,
    zoomControl: true
}});

L.tileLayer('{OsmTiles}', {{
    attribution: '\u00a9 OpenStreetMap',
    maxZoom: 19
}}).addTo(map);

window.getCenter = function() {{
    var c = map.getCenter();
    return c.lat.toFixed(6) + ',' + c.lng.toFixed(6);
}};

window.setCenter = function(lat, lng) {{
    map.panTo([lat, lng]);
}};
</script>
</body>
</html>";
        }

        // ── Bản đồ tuyến đường pickup→dropoff ──────────────────────────────────
        public static string GetRouteMapHtml(
            double pickLat, double pickLon,
            double dropLat, double dropLon,
            string encodedPolyline = "")
        {
            string pLat = pickLat.ToString(CultureInfo.InvariantCulture);
            string pLon = pickLon.ToString(CultureInfo.InvariantCulture);
            string dLat = dropLat.ToString(CultureInfo.InvariantCulture);
            string dLon = dropLon.ToString(CultureInfo.InvariantCulture);
            string polylineJson = System.Text.Json.JsonSerializer.Serialize(encodedPolyline);

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""initial-scale=1, width=device-width"">
<link rel=""stylesheet"" href=""{LeafletCss}""/>
<style>
* {{ margin:0; padding:0; }}
html, body {{ width:100%; height:100%; overflow:hidden; background:#E4DAC8; }}
#map {{ width:100%; height:100%; }}
</style>
</head>
<body>
<div id=""map""></div>
<script src=""{LeafletJs}""></script>
<script>
var pLat = {pLat}, pLon = {pLon};
var dLat = {dLat}, dLon = {dLon};

var map = L.map('map', {{
    zoomControl: false,
    dragging: false,
    scrollWheelZoom: false,
    doubleClickZoom: false,
    touchZoom: false
}});

L.tileLayer('{OsmTiles}', {{
    attribution: '\u00a9 OpenStreetMap',
    maxZoom: 19
}}).addTo(map);

// Marker điểm đón (xanh dương)
var pickupDot = L.divIcon({{
    className: '',
    html: '<div style=""width:16px;height:16px;background:#2196F3;border:3px solid #fff;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,.4)""></div>',
    iconSize: [16,16], iconAnchor: [8,8]
}});
L.marker([pLat, pLon], {{icon: pickupDot}}).addTo(map);

// Marker điểm đến (xanh lá, hình vuông)
var dropoffDot = L.divIcon({{
    className: '',
    html: '<div style=""width:16px;height:16px;background:#00B14F;border:3px solid #fff;border-radius:3px;box-shadow:0 2px 6px rgba(0,0,0,.4)""></div>',
    iconSize: [16,16], iconAnchor: [8,8]
}});
L.marker([dLat, dLon], {{icon: dropoffDot}}).addTo(map);

function drawRoute(coords) {{
    // Viền trắng phía dưới (hiệu ứng Grab)
    L.polyline(coords, {{
        color: '#fff', weight: 10, opacity: 0.6,
        lineCap: 'round', lineJoin: 'round'
    }}).addTo(map);
    // Đường xanh chính
    var poly = L.polyline(coords, {{
        color: '#00B14F', weight: 6, opacity: 0.95,
        lineCap: 'round', lineJoin: 'round'
    }}).addTo(map);
    map.fitBounds(poly.getBounds(), {{ padding: [50, 50] }});
}}

function decodePolyline(encoded) {{
    var pts = [], idx = 0, len = encoded.length, lat = 0, lng = 0;
    while (idx < len) {{
        var b, shift = 0, result = 0;
        do {{ b = encoded.charCodeAt(idx++) - 63; result |= (b & 0x1f) << shift; shift += 5; }} while (b >= 0x20);
        lat += (result & 1) ? ~(result >> 1) : (result >> 1);
        shift = 0; result = 0;
        do {{ b = encoded.charCodeAt(idx++) - 63; result |= (b & 0x1f) << shift; shift += 5; }} while (b >= 0x20);
        lng += (result & 1) ? ~(result >> 1) : (result >> 1);
        pts.push([lat / 1e5, lng / 1e5]);
    }}
    return pts;
}}

function drawFallback() {{
    var encoded = {polylineJson};
    if (encoded && encoded.length > 0) {{
        drawRoute(decodePolyline(encoded));
    }} else {{
        var line = L.polyline([[pLat,pLon],[dLat,dLon]], {{
            color: '#00B14F', weight: 5, opacity: 0.7, dashArray: '10 6',
            lineCap: 'round'
        }}).addTo(map);
        map.fitBounds(line.getBounds(), {{ padding: [50, 50] }});
    }}
}}

// Gọi OSRM để lấy đường thực tế (miễn phí, không cần API key)
var osrmUrl = 'https://router.project-osrm.org/route/v1/driving/'
    + pLon + ',' + pLat + ';' + dLon + ',' + dLat
    + '?overview=full&geometries=geojson';

fetch(osrmUrl)
    .then(function(r) {{ return r.json(); }})
    .then(function(data) {{
        if (data.code === 'Ok' && data.routes && data.routes.length > 0) {{
            var coords = data.routes[0].geometry.coordinates.map(function(c) {{
                return [c[1], c[0]];
            }});
            drawRoute(coords);
        }} else {{
            drawFallback();
        }}
    }})
    .catch(function() {{ drawFallback(); }});

// Center tạm để map hiện trong khi chờ route
map.setView([(pLat + dLat) / 2, (pLon + dLon) / 2], 13);
</script>
</body>
</html>";
        }

        // ── Bản đồ chuyến đang chạy (tài xế + điểm đón/đến) ──────────────────
        public static string GetActiveTripMapHtml(
            double pickLat, double pickLon,
            double dropLat, double dropLon,
            string encodedPolyline = "")
        {
            string pLat = pickLat.ToString(CultureInfo.InvariantCulture);
            string pLon = pickLon.ToString(CultureInfo.InvariantCulture);
            string dLat = dropLat.ToString(CultureInfo.InvariantCulture);
            string dLon = dropLon.ToString(CultureInfo.InvariantCulture);
            string polylineJson = System.Text.Json.JsonSerializer.Serialize(encodedPolyline);

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""initial-scale=1, width=device-width"">
<link rel=""stylesheet"" href=""{LeafletCss}""/>
<style>
* {{ margin:0; padding:0; }}
html, body {{ width:100%; height:100%; overflow:hidden; background:#E4DAC8; }}
#map {{ width:100%; height:100%; }}
</style>
</head>
<body>
<div id=""map""></div>
<script src=""{LeafletJs}""></script>
<script>
var pLat = {pLat}, pLon = {pLon};
var dLat = {dLat}, dLon = {dLon};

var map = L.map('map', {{
    center: [pLat, pLon],
    zoom: 14,
    zoomControl: true
}});

L.tileLayer('{OsmTiles}', {{
    attribution: '\u00a9 OpenStreetMap',
    maxZoom: 19
}}).addTo(map);

// Marker điểm đón
var pickupDot = L.divIcon({{
    className: '',
    html: '<div style=""width:16px;height:16px;background:#2196F3;border:3px solid #fff;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,.4)""></div>',
    iconSize: [16,16], iconAnchor: [8,8]
}});
L.marker([pLat, pLon], {{icon: pickupDot}}).addTo(map);

// Marker điểm đến
var dropoffDot = L.divIcon({{
    className: '',
    html: '<div style=""width:16px;height:16px;background:#00B14F;border:3px solid #fff;border-radius:3px;box-shadow:0 2px 6px rgba(0,0,0,.4)""></div>',
    iconSize: [16,16], iconAnchor: [8,8]
}});
L.marker([dLat, dLon], {{icon: dropoffDot}}).addTo(map);

function decodePolyline(encoded) {{
    var pts = [], idx = 0, len = encoded.length, lat = 0, lng = 0;
    while (idx < len) {{
        var b, shift = 0, result = 0;
        do {{ b = encoded.charCodeAt(idx++) - 63; result |= (b & 0x1f) << shift; shift += 5; }} while (b >= 0x20);
        lat += (result & 1) ? ~(result >> 1) : (result >> 1);
        shift = 0; result = 0;
        do {{ b = encoded.charCodeAt(idx++) - 63; result |= (b & 0x1f) << shift; shift += 5; }} while (b >= 0x20);
        lng += (result & 1) ? ~(result >> 1) : (result >> 1);
        pts.push([lat / 1e5, lng / 1e5]);
    }}
    return pts;
}}

function drawRoute(coords) {{
    L.polyline(coords, {{
        color: '#fff', weight: 10, opacity: 0.5,
        lineCap: 'round', lineJoin: 'round'
    }}).addTo(map);
    L.polyline(coords, {{
        color: '#00B14F', weight: 6, opacity: 0.85,
        lineCap: 'round', lineJoin: 'round'
    }}).addTo(map);
}}

// Lấy đường đi thực tế từ OSRM
var osrmUrl = 'https://router.project-osrm.org/route/v1/driving/'
    + pLon + ',' + pLat + ';' + dLon + ',' + dLat
    + '?overview=full&geometries=geojson';

fetch(osrmUrl)
    .then(function(r) {{ return r.json(); }})
    .then(function(data) {{
        if (data.code === 'Ok' && data.routes && data.routes.length > 0) {{
            var coords = data.routes[0].geometry.coordinates.map(function(c) {{
                return [c[1], c[0]];
            }});
            drawRoute(coords);
        }} else {{
            var encoded = {polylineJson};
            if (encoded && encoded.length > 0) drawRoute(decodePolyline(encoded));
        }}
    }})
    .catch(function() {{
        var encoded = {polylineJson};
        if (encoded && encoded.length > 0) drawRoute(decodePolyline(encoded));
    }});

// Marker tài xế (xe máy vàng)
var driverIcon = L.divIcon({{
    className: '',
    html: '<div style=""width:32px;height:32px;background:#FFC107;border:3px solid #fff;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:16px;box-shadow:0 2px 8px rgba(0,0,0,.35)"">🛵</div>',
    iconSize: [32, 32],
    iconAnchor: [16, 16]
}});

var _driverMarker = L.marker([pLat, pLon], {{icon: driverIcon}}).addTo(map);

window.updateDriver = function(lat, lng) {{
    _driverMarker.setLatLng([lat, lng]);
    map.panTo([lat, lng]);
}};
</script>
</body>
</html>";
        }
    }
}
