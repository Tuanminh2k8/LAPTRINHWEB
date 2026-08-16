(function(){
  'use strict';
  const root = document.getElementById('salesVariantManager');
  if(!root) return;

  const initial = JSON.parse(document.getElementById('salesVariantInitialData')?.textContent || '{}');
  const typeList = root.querySelector('#variantTypeList');
  const body = root.querySelector('#variantMatrixBody');

  const state = {
    types: (initial.variantTypes || []).map((t, index) => ({
      id: cryptoRandom(),
      name: t.name || `Phân loại ${index+1}`,
      presets: t.presets || [],
      selectedPreset: t.selectedPreset || '',
      options: (t.options || []).map((v,i)=>{
        const parts = String(v).split('|');
        return { id: cryptoRandom(), name: parts[0], desc: parts[1] || '' };
      })
    })),
    matrixExpanded: true,
    rows: {}
  };

  function cryptoRandom(){
    if(window.crypto?.randomUUID) return crypto.randomUUID();
    return 'id-' + Date.now() + '-' + Math.random().toString(36).slice(2);
  }

  function esc(s){return String(s ?? '').replace(/[&<>'"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));}

  function renderTypes(){
    typeList.innerHTML='';
    state.types.forEach((type, typeIndex)=>{
      const tpl = document.getElementById('variantTypeTemplate').content.cloneNode(true);
      const card = tpl.querySelector('.svm__type-card');
      tpl.querySelector('.svm__type-label').textContent = `Phân loại${typeIndex+1}`;
      const name = tpl.querySelector('.svm__type-name');
      name.value = type.name;
      name.addEventListener('input', e=>{type.name=e.target.value; renderMatrix();});
      tpl.querySelector('.type-delete').addEventListener('click', ()=>{
        if(state.types.length<=1) return;
        state.types.splice(typeIndex,1);
        renderTypes(); renderMatrix();
      });
      const presetRow = tpl.querySelector('.svm__preset-row');
      type.presets.forEach(p=>{
        const label=document.createElement('label');
        label.className='svm__preset';
        label.innerHTML=`<input type="radio" name="preset-${type.id}" ${type.selectedPreset===p?'checked':''}> <span>${esc(p)}</span>`;
        label.querySelector('input').addEventListener('change',()=>type.selectedPreset=p);
        presetRow.appendChild(label);
      });
      const grid = tpl.querySelector('.svm__option-grid');
      type.options.forEach((opt, oi)=>{
        const row=document.createElement('div'); row.className='svm__option';
        row.innerHTML=`<input value="${esc(opt.name)}" aria-label="Tên tùy chọn"><input value="${esc(opt.desc)}" placeholder="Thêm mô tả" aria-label="Mô tả"><button class="svm__drag" title="Kéo">✣</button><button class="svm__delete-option" title="Xóa">▢</button>`;
        const [nameInput,descInput]=row.querySelectorAll('input');
        nameInput.addEventListener('input',e=>{opt.name=e.target.value; renderMatrix();});
        descInput.addEventListener('input',e=>opt.desc=e.target.value);
        row.querySelector('.svm__delete-option').addEventListener('click',()=>{type.options.splice(oi,1); renderTypes(); renderMatrix();});
        grid.appendChild(row);
      });
      const add=document.createElement('button'); add.type='button'; add.className='svm__add-option'; add.textContent='＋ Thêm tùy chọn';
      add.addEventListener('click',()=>{type.options.push({id:cryptoRandom(),name:'',desc:''});renderTypes();renderMatrix();});
      grid.appendChild(add);
      typeList.appendChild(tpl);
    });
  }

  function getVariantOptions(){
    const primary=state.types[0]?.options?.filter(x=>x.name.trim()) || [];
    const addons=state.types[1]?.options?.filter(x=>x.name.trim()) || [];
    return {primary, addons};
  }

  function rowKey(primary, addon){return `${primary.id}__${addon.id}`;}

  function renderMatrix(){
    const {primary, addons}=getVariantOptions();
    body.innerHTML='';
    primary.forEach((p, pi)=>{
      const items = addons.length ? addons : [{id:'empty-addon',name:'Thêm món'}];
      items.forEach((a, ai)=>{
        const key=rowKey(p,a);
        const data=state.rows[key] ||= {price:'',stock:'0',sku:'',image:''};
        const tr=document.createElement('tr');
        if(ai===0){
          const variant=document.createElement('td'); variant.className='svm__group-cell'; variant.rowSpan=items.length;
          variant.innerHTML=`<div class="svm__variant-name">${esc(p.name)}</div><label class="svm__upload"><span>▧</span><input type="file" accept="image/*"></label>`;
          const file=variant.querySelector('input');
          const upload=variant.querySelector('.svm__upload');
          if(data.image){upload.innerHTML=`<img src="${data.image}" alt="">`;}
          file.addEventListener('change',e=>{const f=e.target.files?.[0];if(!f)return; const r=new FileReader();r.onload=()=>{data.image=r.result;renderMatrix();};r.readAsDataURL(f);});
          tr.appendChild(variant);
        }
        const addon=document.createElement('td'); addon.className='svm__addon-cell'; addon.textContent=a.name || 'Thêm món'; tr.appendChild(addon);

        const price=document.createElement('td');
        price.innerHTML=`<input class="svm__price-input ${data.price===''?'error':''}" inputmode="numeric" placeholder="Input" value="${esc(data.price)}"><div class="svm__error" ${data.price===''?'':'style="display:none"'}>Không được để trống ô</div>`;
        const priceInput=price.querySelector('input'); const err=price.querySelector('.svm__error');
        priceInput.addEventListener('input',e=>{data.price=e.target.value;priceInput.classList.toggle('error',!e.target.value);err.style.display=e.target.value?'none':'';}); tr.appendChild(price);

        const stock=document.createElement('td');stock.innerHTML=`<input class="svm__cell-input" inputmode="numeric" value="${esc(data.stock)}">`;stock.querySelector('input').addEventListener('input',e=>data.stock=e.target.value);tr.appendChild(stock);
        const sku=document.createElement('td');sku.innerHTML=`<input class="svm__cell-input" placeholder="Nhập" value="${esc(data.sku)}">`;sku.querySelector('input').addEventListener('input',e=>data.sku=e.target.value);tr.appendChild(sku);
        body.appendChild(tr);
      });
    });
  }

  root.querySelector('#applyAllBtn').addEventListener('click',()=>{
    const price=root.querySelector('#bulkPrice').value.trim();
    const stock=root.querySelector('#bulkStock').value.trim();
    const sku=root.querySelector('#bulkSku').value.trim();
    const {primary,addons}=getVariantOptions();
    primary.forEach(p=> (addons.length?addons:[{id:'empty-addon'}]).forEach(a=>{const d=state.rows[rowKey(p,a)] ||= {};if(price)d.price=price;if(stock)d.stock=stock;if(sku)d.sku=sku;}));
    renderMatrix();
  });

  root.querySelector('#collapseMatrix').addEventListener('click',()=>{
    state.matrixExpanded=!state.matrixExpanded;
    root.querySelector('.svm__table-wrap').style.display=state.matrixExpanded?'':'none';
    root.querySelector('#collapseMatrix').innerHTML=state.matrixExpanded?'<span>Ẩn</span>⌃':'<span>Hiện</span>⌄';
  });

  root.querySelector('#newSizeChartBtn').addEventListener('click',()=>{
    root.querySelector('#sizeTemplate').value='clothing';
    root.querySelector('#sizeError').style.display='none';
  });

  root.querySelector('#sizeTemplate').addEventListener('change',e=>{
    root.querySelector('#sizeError').style.display=e.target.value?'none':'';
  });

  renderTypes();
  renderMatrix();

  window.SalesVariantManager={
    getData(){
      return {variantTypes:state.types.map(t=>({name:t.name,selectedPreset:t.selectedPreset,options:t.options.map(o=>({name:o.name,description:o.desc}))})),rows:JSON.parse(JSON.stringify(state.rows)),sizeTemplate:root.querySelector('#sizeTemplate').value};
    },
    setData(data){
      if(!data) return;
      if(Array.isArray(data.variantTypes)) state.types=data.variantTypes.map((t,i)=>({id:cryptoRandom(),name:t.name||`Phân loại${i+1}`,presets:t.presets||[],selectedPreset:t.selectedPreset||'',options:(t.options||[]).map(o=>({id:cryptoRandom(),name:o.name||o,desc:o.description||''}))}));
      if(data.rows) Object.assign(state.rows,data.rows);
      renderTypes();renderMatrix();
    }
  };
})();
