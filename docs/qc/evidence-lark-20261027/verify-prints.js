/** Evidence 5 phieu in khop Figma — tai PDF that tu staging, luu vao ./pdf/, ghi EVIDENCE-PRINTS.txt
 * Chay: BASE_URL=https://his.diab.vn node docs/qc/evidence-lark-20261027/verify-prints.js */
const path = require("path");
const fs = require("fs");
const REPO_ROOT = path.resolve(__dirname, "../../..");
const { chromium } = require(path.join(REPO_ROOT, "frontend", "node_modules", "@playwright", "test"));
const BASE = (process.env.BASE_URL || "https://his.diab.vn").replace(/\/$/, "");
const OUTDIR = path.join(__dirname, "pdf");
const OUT = path.join(__dirname, "EVIDENCE-PRINTS.txt");
fs.mkdirSync(OUTDIR, { recursive: true });

const BILLING = "f88e4ced-b984-433b-8704-80c84c752597";
const PAYMENT = "3fc36aaa-2162-47fe-a6bd-198933ee8a78";
const PRESC = "de4d792b-8752-46f3-a010-a7c3fb2df54c";
const ENC_LAB = "0efa6b11-dd40-4ae2-b7a4-6fa4ffbb562e";
const ENC_RAD = "5dc2557f-e7eb-492b-a143-6adcbf59793f";

const lines = [];
function rec(s){ lines.push(s); console.log(s); }

async function quickLogin(page, role){
  for(let i=1;i<=3;i++){
    await page.goto(BASE+"/login",{waitUntil:"domcontentloaded"});
    await page.waitForLoadState("networkidle").catch(()=>{});
    await page.waitForTimeout(1200);
    try{ await page.getByRole("button",{name:role,exact:true}).click(); }catch{ return false; }
    try{ await page.waitForURL(u=>!u.pathname.includes("/login"),{timeout:30000}); return true; }catch{}
  }
  return false;
}
async function getToken(page){
  return page.evaluate(()=>{ for(let i=0;i<localStorage.length;i++){const v=localStorage.getItem(localStorage.key(i));if(!v)continue;const m=v.match(/"accessToken":"(eyJ[^"]+)"/);if(m)return m[1];const b=v.match(/eyJ[A-Za-z0-9_.-]+/);if(b)return b[0];} return null; });
}
async function fetchPdf(page, token, url, method){
  return page.evaluate(async ({url,token,method})=>{
    const res = await fetch(url,{method,headers:{Authorization:"Bearer "+token}});
    const ct = res.headers.get("content-type")||"";
    const buf = await res.arrayBuffer();
    const bytes = new Uint8Array(buf); let b=""; for(let i=0;i<bytes.length;i++) b+=String.fromCharCode(bytes[i]);
    return { status:res.status, ct, b64: btoa(b), size: bytes.length };
  }, {url,token,method});
}
async function grab(page, token, label, file, url, method){
  try{
    const r = await fetchPdf(page, token, url, method);
    const ok = r.status===200 && r.ct.includes("application/pdf") && r.size>1500;
    if(ok) fs.writeFileSync(path.join(OUTDIR,file), Buffer.from(r.b64,"base64"));
    rec(`${ok?"PASS":"----"}  ${label}: ${method} ${url.replace(BASE,"")} -> status=${r.status}, ct=${r.ct}, bytes=${r.size}${ok?` -> pdf/${file}`:""}`);
  }catch(e){ rec(`ERR   ${label}: ${e.message}`); }
}

(async()=>{
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ignoreHTTPSErrors:true});
  const page = await ctx.newPage();
  try{
    await quickLogin(page,"Kế toán");
    let t = await getToken(page);
    await grab(page,t,"Phieu thu tam","01-phieu-thu-tam.pdf", `${BASE}/api/v1/billings/${BILLING}/provisional-receipt`, "POST");
    await grab(page,t,"Bang ke chi phi","02-bang-ke-chi-phi.pdf", `${BASE}/api/v1/billings/${BILLING}/cost-statement`, "POST");
    await grab(page,t,"Phieu thu chinh thuc (hoa don)","03-hoa-don-dich-vu.pdf", `${BASE}/api/v1/billings/${BILLING}/pdf`, "GET");
    await grab(page,t,"Bien lai thu tien (official receipt)","04-bien-lai-thu-tien.pdf", `${BASE}/api/v1/cashier/receipts/${PAYMENT}/print`, "POST");

    await quickLogin(page,"Bác sĩ");
    t = await getToken(page);
    await grab(page,t,"Don thuoc","05-don-thuoc.pdf", `${BASE}/api/v1/prescriptions/${PRESC}/pdf`, "GET");
    await grab(page,t,"Phieu chi dinh XN","06-chi-dinh-xet-nghiem.pdf", `${BASE}/api/v1/encounters/${ENC_LAB}/lab-orders/pdf`, "GET");
    await grab(page,t,"Phieu chi dinh CDHA","07-chi-dinh-cdha.pdf", `${BASE}/api/v1/encounters/${ENC_RAD}/rad-orders/pdf`, "GET");
  } finally {
    fs.writeFileSync(OUT, lines.join("\n")+"\n","utf8");
    console.log("\nDa ghi "+OUT);
    await browser.close();
  }
})();
