import { arr, rentalsApi, contractsApi, paymentsApi, extensionsApi, cancellationsApi, handoversApi, returnsApi, evaluationsApi, adminApi } from './api';

const dt = v => v ? new Date(v).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',hour:'2-digit',minute:'2-digit'}) : 'Vừa cập nhật';
const stamp = x => dt(x?.updatedAt||x?.updated_at||x?.createdAt||x?.created_at||x?.thoiGianTao||x?.ngayTao||x?.thoiGianCapNhat);
const n = (id,title,message,target,tone='info',timeLabel='Vừa cập nhật') => ({id,title,message,target,tone,timeLabel});
const statusLabel = s => ({PENDING:'chờ xử lý',APPROVED:'đã được duyệt',REJECTED:'đã bị từ chối',CANCELLED:'đã hủy',DRAFT:'đã tạo nháp',SENT:'đã gửi cho khách',CUSTOMER_CONFIRMED:'khách đã xác nhận',WAITING_CONFIRMATION:'chờ xác nhận thanh toán',PAID:'đã thanh toán',READY_FOR_PICKUP:'sẵn sàng giao xe',WAITING_CUSTOMER_RECEIVE:'chờ khách nhận xe',IN_PROGRESS:'đang thuê',RETURNED:'đã trả xe/chờ quyết toán',COMPLETED:'đã hoàn tất',REFUND_PENDING:'chờ hoàn tiền',REFUNDED:'đã hoàn tiền'}[s]||s);

async function contractDetails(contracts, mine=false){
  const payments=[],extensions=[],cancellations=[],handovers=[],returns=[];
  await Promise.all(contracts.map(async c=>{
    const id=c.id;
    const [p,e,cn,h,r,rr]=await Promise.all([
      paymentsApi.byContract(id).catch(()=>[]),
      (mine?extensionsApi.myByContract(id):extensionsApi.byContract(id)).catch(()=>[]),
      (mine?cancellationsApi.myByContract(id):cancellationsApi.byContract(id)).catch(()=>[]),
      handoversApi.byContract(id).catch(()=>null),returnsApi.byContract(id).catch(()=>null),returnsApi.requestByContract(id).catch(()=>null)
    ]);
    arr(p).forEach(x=>payments.push({...x,idHopDong:x.idHopDong||id}));
    arr(e).forEach(x=>extensions.push({...x,idHopDong:x.idHopDong||id}));
    arr(cn).forEach(x=>cancellations.push({...x,idHopDong:x.idHopDong||id}));
    if(h) handovers.push({...h,idHopDong:h.idHopDong||id});
    if(r) returns.push({...r,idHopDong:r.idHopDong||id});
    if(rr) returns.push({...rr,idHopDong:rr.idHopDong||id,_request:true});
  }));
  return {payments,extensions,cancellations,handovers,returns};
}

export async function pollStaffFlow(){
  const [rentalsRaw,contractsRaw]=await Promise.all([rentalsApi.list('page=1&pageSize=100'),contractsApi.list()]);
  const rentals=arr(rentalsRaw), contracts=arr(contractsRaw);
  const d=await contractDetails(contracts,false);
  const notifications=[];
  rentals.forEach(x=>{ if(x.trangThai==='PENDING') notifications.push(n(`staff-rental-${x.id}-PENDING`,`Yêu cầu thuê mới #${x.id}`,'Khách hàng vừa gửi yêu cầu thuê mới.','requests','warning',stamp(x))); });
  contracts.forEach(x=>{ if(x.trangThai==='CUSTOMER_CONFIRMED') notifications.push(n(`staff-contract-${x.id}-CUSTOMER_CONFIRMED`,`Khách đã xác nhận hợp đồng #${x.id}`,'Hợp đồng đã được khách xác nhận, kiểm tra bước thanh toán tiếp theo.','contracts','success',stamp(x))); if(x.trangThai==='WAITING_CUSTOMER_RECEIVE') notifications.push(n(`staff-contract-${x.id}-WAITING_CUSTOMER_RECEIVE`,`Đang chờ khách nhận xe #${x.id}`,'Xe đã bàn giao và đang chờ khách xác nhận nhận xe.','handover','info',stamp(x))); });
  d.payments.forEach(x=>{ if(['WAITING_CONFIRMATION','CUSTOMER_PAID','PENDING'].includes(x.trangThai)) notifications.push(n(`staff-payment-${x.id}-${x.trangThai}`,`Thanh toán mới cần xác nhận #${x.id}`,`Khách vừa thao tác thanh toán ${x.loaiThanhToan||''}. Mở Thanh toán để xử lý.`,'payments','warning',stamp(x))); if(x.trangThai==='REFUND_PENDING') notifications.push(n(`staff-refund-${x.id}-REFUND_PENDING`,`Có khoản hoàn tiền cần xử lý #${x.id}`,'Mở Thanh toán/Hủy hợp đồng để xử lý hoàn tiền.','payments','warning',stamp(x))); });
  d.extensions.forEach(x=>{ if(x.trangThai==='PENDING') notifications.push(n(`staff-extension-${x.id}-PENDING`,`Yêu cầu gia hạn mới #${x.id}`,'Khách hàng vừa gửi yêu cầu gia hạn hợp đồng.','renewal','warning',stamp(x))); });
  d.cancellations.forEach(x=>{ if(x.trangThai==='PENDING') notifications.push(n(`staff-cancel-${x.id}-PENDING`,`Yêu cầu hủy mới #${x.id}`,'Khách hàng vừa gửi yêu cầu hủy hợp đồng.','cancel','warning',stamp(x))); });
  d.returns.filter(x=>x._request).forEach(x=>notifications.push(n(`staff-return-request-${x.id||x.idHopDong}-${x.trangThai||'NEW'}`,`Khách yêu cầu trả xe · HĐ #${x.idHopDong}`,'Có yêu cầu trả xe mới cần nhân viên tiếp nhận và kiểm tra.','return','warning',stamp(x))));
  return {notifications,data:{rentals,contracts,...d}};
}

export async function pollCustomerFlow(){
  const rentals=arr(await rentalsApi.discoverMine());
  const allContracts=arr(await contractsApi.discover());
  const rentalIds=new Set(rentals.map(x=>Number(x.id)));
  const contracts=allContracts.filter(c=>rentalIds.has(Number(c.idYeuCauThue)));
  const d=await contractDetails(contracts,true);
  const notifications=[];
  rentals.forEach(x=>{ if(['APPROVED','REJECTED','CANCELLED'].includes(x.trangThai)) notifications.push(n(`customer-rental-${x.id}-${x.trangThai}`,`Yêu cầu #${x.id} ${statusLabel(x.trangThai)}`,x.trangThai==='APPROVED'?'Nhân viên đã duyệt yêu cầu thuê của bạn.':'Trạng thái yêu cầu thuê vừa được cập nhật.','tracking',x.trangThai==='APPROVED'?'success':'warning',stamp(x))); });
  contracts.forEach(x=>{ if(['SENT','PAID','READY_FOR_PICKUP','WAITING_CUSTOMER_RECEIVE','IN_PROGRESS','RETURNED','COMPLETED','CANCELLED'].includes(x.trangThai)) notifications.push(n(`customer-contract-${x.id}-${x.trangThai}`,`Hợp đồng #${x.id} ${statusLabel(x.trangThai)}`,x.trangThai==='SENT'?'Hợp đồng mới đang chờ bạn xem và xác nhận.':x.trangThai==='RETURNED'?'Xe đã được kiểm tra. Hãy xem khoản quyết toán cuối.':x.trangThai==='COMPLETED'?'Hợp đồng đã hoàn tất. Bạn có thể đánh giá dịch vụ.':'Hành trình thuê xe của bạn vừa được cập nhật.','tracking',x.trangThai==='COMPLETED'?'success':'info',stamp(x))); });
  d.payments.forEach(x=>{ if(['PAID','FAILED','REFUNDED','REFUND_PENDING'].includes(x.trangThai)) notifications.push(n(`customer-payment-${x.id}-${x.trangThai}`,`Thanh toán #${x.id} ${statusLabel(x.trangThai)}`,x.trangThai==='PAID'?'Nhân viên đã xác nhận khoản thanh toán của bạn.':x.trangThai==='REFUNDED'?'Khoản hoàn tiền đã được xác nhận.':'Trạng thái thanh toán vừa được cập nhật.','tracking',x.trangThai==='PAID'||x.trangThai==='REFUNDED'?'success':'warning',stamp(x))); });
  d.extensions.forEach(x=>{ if(['APPROVED','REJECTED'].includes(x.trangThai)) notifications.push(n(`customer-extension-${x.id}-${x.trangThai}`,`Gia hạn #${x.id} ${statusLabel(x.trangThai)}`,'Nhân viên đã xử lý yêu cầu gia hạn của bạn.','tracking',x.trangThai==='APPROVED'?'success':'warning',stamp(x))); });
  d.cancellations.forEach(x=>{ if(['APPROVED','REJECTED'].includes(x.trangThai)) notifications.push(n(`customer-cancel-${x.id}-${x.trangThai}`,`Yêu cầu hủy #${x.id} ${statusLabel(x.trangThai)}`,'Nhân viên đã xử lý yêu cầu hủy hợp đồng của bạn.','tracking',x.trangThai==='APPROVED'?'success':'warning',stamp(x))); });
  return {notifications,data:{rentals,contracts,...d}};
}

export async function pollAdminFlow(){
  const [usersRaw,reviewsRaw,auditsRaw]=await Promise.all([adminApi.users(),evaluationsApi.list(),adminApi.audits().catch(()=>[])]);
  const users=arr(usersRaw),reviews=arr(reviewsRaw),audits=arr(auditsRaw);
  const notifications=[];
  reviews.forEach(x=>notifications.push(n(`admin-review-${x.id}`,`Đánh giá mới #${x.id}`,'Khách hàng vừa gửi đánh giá. Mở quản lý đánh giá để kiểm tra.','reviews','success',stamp(x))));
  audits.slice(-20).forEach(x=>{ if(['MAINTENANCE','INACTIVE'].some(k=>String(x.moTa||x.hanhDong||'').toUpperCase().includes(k))) notifications.push(n(`admin-audit-${x.id}`,`Cập nhật vận hành #${x.id}`,x.moTa||'Có thay đổi trạng thái cần quản trị viên theo dõi.','audit','warning',stamp(x))); });
  return {notifications,data:{users,reviews,audits}};
}
