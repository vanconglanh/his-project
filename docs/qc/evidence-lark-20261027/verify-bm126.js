/** BM-126 verify: POST /billings/{id}/provisional-receipt tra PDF (A5 phieu thu tam).
 * Chay: BASE_URL=https://his.diab.vn node docs/qc/evidence-lark-20261027/verify-bm126.js */
const path = require("path");
const fs = require("fs");
const REPO_ROOT = path.resolve(__dirname, "../../..");
const { chromium } = require(path.join(REPO_ROOT, "frontend", "node_modules", "@playwright", "test"));
const BASE = (process.env.BASE_URL || "https://his.diab.vn").replace(/\/$/, "");
const OUT = path.join(__dirname, "EVIDENCE-BM126.txt");
const BILLING_ID = "f88e4ced-b984-433b-8704-80c84c752597"; // DRAFT billing tenant 2 (payable 8.4tr)
const lines = [];
function rec(s){ lines.push(s); console.log(s); }

async function quickLogin(page, roleLabel){
  for(let i=1;i<=3;i++){
    await page.goto(BASE+"/login",{waitUntil:"domcontentloaded"});
    await page.waitForLoadState("networkidle").catch(()=>{});
    await page.waitForTimeout(1200);
    try{ await page.getByRole("button",{name:roleLabel,exact:true}).click(); }catch{ return false; }
    try{ await page.waitForURL(u=>!u.pathname.includes("/login"),{timeout:30000}); return true; }catch{}
  }
  return false;
}
async function getToken(page){
  return page.evaluate(()=>{ for(let i=0;i<localStorage.length;i++){const v=localStorage.getItem(localStorage.key(i));if(!v)continue;const m=v.match(/"accessToken":"(eyJ[^"]+)"/);if(m)return m[1];const b=v.match(/eyJ[A-Za-z0-9_.-]+/);if(b)return b[0];} return null; });
}
async function probe(page, token){
  return page.evaluate(async ({url,token})=>{
    const res = await fetch(url,{method:"POST",headers:{Authorization:"Bearer "+token}});
    const ct = res.headers.get("content-type")||"";
    let size=0; try{ const b=await res.blob(); size=b.size; }catch{}
    return { status:res.status, ct, size };
  }, { url:`/api/v1/billings/${BILLING_ID}/provisional-receipt`, token });
}

(async()=>{
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ignoreHTTPSErrors:true});
  const page = await ctx.newPage();
  try{
    for(const role of ["Lễ tân","Kế toán","Bác sĩ","Kỹ thuật viên"]){
      const ok = await quickLogin(page, role);
      if(!ok){ rec(`SKIP  ${role}: khong dang nhap duoc`); continue; }
      const t = await getToken(page);
      const r = await probe(page, t);
      const pass = r.status===200 && r.ct.includes("application/pdf") && r.size>1000;
      rec(`${pass?"PASS":"----"}  BM-126 role=${role}: POST provisional-receipt status=${r.status}, content-type=${r.ct}, pdfBytes=${r.size}`);
      if(pass) break;
    }
  } finally {
    fs.writeFileSync(OUT, lines.join("\n")+"\n","utf8");
    console.log("\nDa ghi "+OUT);
    await browser.close();
  }
})();
