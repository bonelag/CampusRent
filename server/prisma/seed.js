import { PrismaClient } from '@prisma/client';
import bcrypt from 'bcryptjs';

const prisma = new PrismaClient();

async function main() {
  console.log('[SEED] Bắt đầu dọn dẹp dữ liệu cũ (nếu có)...');
  await prisma.report.deleteMany();
  await prisma.review.deleteMany();
  await prisma.post.deleteMany();
  await prisma.user.deleteMany();

  console.log('[SEED] Tạo tài khoản mẫu...');
  const salt = bcrypt.genSaltSync(10);
  const commonPassword = bcrypt.hashSync('Password@123', salt);

  const admin = await prisma.user.create({
    data: {
      email: 'admin@campusrent.vn',
      password: commonPassword,
      fullName: 'Quản Trị Viên',
      phoneNumber: '0901234567',
      role: 'ADMIN',
      isEduVerified: false,
    },
  });

  const landlord1 = await prisma.user.create({
    data: {
      email: 'landlord1@gmail.com',
      password: commonPassword,
      fullName: 'Bác Ba Chủ Trọ',
      phoneNumber: '0912345678',
      role: 'LANDLORD',
      isEduVerified: false,
    },
  });

  const landlord2 = await prisma.user.create({
    data: {
      email: 'landlord2@gmail.com',
      password: commonPassword,
      fullName: 'Cô Mai Cầu Giấy',
      phoneNumber: '0987654321',
      role: 'LANDLORD',
      isEduVerified: false,
    },
  });

  const studentEdu = await prisma.user.create({
    data: {
      email: 'nguyenvanan@sis.hust.edu.vn',
      password: commonPassword,
      fullName: 'Nguyễn Văn An (BKHN)',
      phoneNumber: '0933112233',
      role: 'STUDENT',
      isEduVerified: true,
    },
  });

  const studentNormal = await prisma.user.create({
    data: {
      email: 'tranthib@gmail.com',
      password: commonPassword,
      fullName: 'Trần Thị Bích',
      phoneNumber: '0944556677',
      role: 'STUDENT',
      isEduVerified: false,
    },
  });

  console.log('[SEED] Chuẩn bị tọa độ các cụm khu vực trọng điểm...');
  const targetHubs = [
    {
      name: 'Khu vực Bách Khoa (Hai Bà Trưng, HN)',
      latitude: 21.0044,
      longitude: 105.8433,
    },
    {
      name: 'Khu vực Kinh tế Quốc dân (Hai Bà Trưng, HN)',
      latitude: 21.0003,
      longitude: 105.8427,
    },
    {
      name: 'Khu vực Cầu Giấy - ĐHQGHN',
      latitude: 21.0378,
      longitude: 105.7828,
    },
    {
      name: 'Khu vực Quận 10 (TP.HCM)',
      latitude: 10.7725,
      longitude: 106.6578,
    },
    {
      name: 'Khu vực Làng Đại học Thủ Đức (TP.HCM)',
      latitude: 10.8700,
      longitude: 106.8031,
    },
  ];

  const sampleImages = [
    'https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1598928506311-c55ded91a20c?auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=800&q=80',
  ];

  console.log('[SEED] Tạo 30 phòng trọ thực tế quanh 5 trường...');
  const roomsSpec = [
    // 6 phòng quanh BKHN (Hai Bà Trưng, HN)
    {
      uniIdx: 0,
      title: 'Phòng khép kín ban công thoáng ngõ Tạ Quang Bửu',
      description: 'Phòng sạch đẹp ngay sát KTX Bách Khoa, có điều hòa, nóng lạnh, giờ giấc tự do.',
      address: 'Ngõ 27 Tạ Quang Bửu, Bách Khoa, Hai Bà Trưng, Hà Nội',
      latOffset: 0.002,
      lngOffset: 0.001,
      basePrice: 3200000,
      area: 22,
      amenities: ['wifi', 'ac', 'heater', 'balcony'],
    },
    {
      uniIdx: 0,
      title: 'Phòng trọ giá rẻ cho sinh viên phố Lê Thanh Nghị',
      description: 'Phòng tầng 2 nhà riêng, chung chủ hiền lành, an ninh tốt, gần chợ.',
      address: 'Số 58 Lê Thanh Nghị, Bách Khoa, Hai Bà Trưng, Hà Nội',
      latOffset: -0.002,
      lngOffset: 0.002,
      basePrice: 2400000,
      area: 18,
      amenities: ['wifi', 'heater', 'parking'],
    },
    {
      uniIdx: 0,
      title: 'Chung cư mini cao cấp phố Trần Đại Nghĩa có thang máy',
      description: 'Full nội thất: giường tủ, tủ lạnh, bếp từ, máy giặt riêng, khóa vân tay.',
      address: '112 Trần Đại Nghĩa, Đồng Tâm, Hai Bà Trưng, Hà Nội',
      latOffset: -0.003,
      lngOffset: -0.001,
      basePrice: 4500000,
      area: 28,
      amenities: ['wifi', 'ac', 'parking', 'heater', 'balcony'],
    },
    {
      uniIdx: 0,
      title: 'Studio full đồ ngõ Đại La gần trường',
      description: 'Phòng mới sửa sang, thoáng mát, khu dân trí cao, yên tĩnh học tập.',
      address: 'Ngõ 128 Đại La, Đồng Tâm, Hai Bà Trưng, Hà Nội',
      latOffset: -0.005,
      lngOffset: 0.003,
      basePrice: 3800000,
      area: 25,
      amenities: ['wifi', 'ac', 'heater'],
    },
    {
      uniIdx: 0,
      title: 'Phòng ở ghép homestay view công viên Thống Nhất',
      description: 'Bao trọn chi phí điện nước mạng, có người dọn vệ sinh hàng tuần.',
      address: 'Phố Nguyễn Đình Chiểu, Lê Đại Hành, Hai Bà Trưng, Hà Nội',
      latOffset: 0.007,
      lngOffset: -0.003,
      basePrice: 1800000,
      area: 30,
      amenities: ['wifi', 'ac', 'parking', 'heater'],
    },
    {
      uniIdx: 0,
      title: 'Căn hộ dịch vụ 1N1K phố Bạch Mai',
      description: 'Thích hợp cho 2 bạn sinh viên ở cùng, có ban công phơi đồ riêng.',
      address: 'Ngõ 300 Bạch Mai, Bạch Mai, Hai Bà Trưng, Hà Nội',
      latOffset: -0.004,
      lngOffset: 0.006,
      basePrice: 4200000,
      area: 32,
      amenities: ['wifi', 'ac', 'parking', 'heater', 'balcony'],
    },

    // 6 phòng quanh NEU (Kinh tế Quốc dân)
    {
      uniIdx: 1,
      title: 'Phòng trọ khép kín ngõ Tự Do - Thiên đường ẩm thực NEU',
      description: 'Cách cổng NEU 100m, ăn uống bạt ngàn, phòng thoáng, có gác xép.',
      address: 'Ngõ Tự Do, Đồng Tâm, Hai Bà Trưng, Hà Nội',
      latOffset: 0.001,
      lngOffset: 0.001,
      basePrice: 2800000,
      area: 20,
      amenities: ['wifi', 'heater', 'parking'],
    },
    {
      uniIdx: 1,
      title: 'CCMN mới xây phố Vọng full đồ xịn',
      description: 'Phòng tầng 4 có thang máy, trang bị máy sấy, bảo vệ 24/7.',
      address: 'Ngõ 41 Phố Vọng, Đồng Tâm, Hai Bà Trưng, Hà Nội',
      latOffset: -0.003,
      lngOffset: 0.003,
      basePrice: 4300000,
      area: 26,
      amenities: ['wifi', 'ac', 'heater', 'parking'],
    },
    {
      uniIdx: 1,
      title: 'Phòng đơn giá hạt dẻ đường Giải Phóng',
      description: 'Phòng nhỏ gọn phù hợp 1 bạn ở, sạch sẽ, xe để tầng 1 miễn phí.',
      address: '250 Giải Phóng, Phương Mai, Đống Đa, Hà Nội',
      latOffset: 0.002,
      lngOffset: -0.003,
      basePrice: 2000000,
      area: 16,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 1,
      title: 'Studio duplex có gác lửng phố Trương Định',
      description: 'Trần cao 4m, ban công đón nắng, không chung chủ, giờ tự do.',
      address: 'Ngõ 60 Trương Định, Giáp Bát, Hoàng Mai, Hà Nội',
      latOffset: -0.006,
      lngOffset: 0.002,
      basePrice: 3500000,
      area: 24,
      amenities: ['wifi', 'ac', 'heater', 'balcony'],
    },
    {
      uniIdx: 1,
      title: 'Phòng khép kín ngõ Cột Cờ Giải Phóng',
      description: 'Khu an ninh cực tốt, gần các tuyến xe bus lớn chạy qua NEU và Bách Khoa.',
      address: 'Ngõ 198 Giải Phóng, Phương Liệt, Thanh Xuân, Hà Nội',
      latOffset: -0.004,
      lngOffset: -0.002,
      basePrice: 2600000,
      area: 19,
      amenities: ['wifi', 'heater', 'parking'],
    },
    {
      uniIdx: 1,
      title: 'Căn hộ 2 phòng ngủ cho nhóm bạn NEU đường Đại La',
      description: 'Căn góc 2 mặt thoáng, có bếp tách riêng không lo ám mùi.',
      address: 'Ngõ 88 Đại La, Trương Định, Hai Bà Trưng, Hà Nội',
      latOffset: -0.002,
      lngOffset: 0.005,
      basePrice: 5500000,
      area: 45,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },

    // 6 phòng quanh UET (Cầu Giấy, HN)
    {
      uniIdx: 2,
      title: 'Phòng trọ khép kín ngõ 175 Xuân Thủy đối diện ĐHQG',
      description: 'Đi bộ sang trường 3 phút, khu sinh viên tấp nập, an ninh đảm bảo.',
      address: 'Ngõ 175 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội',
      latOffset: 0.001,
      lngOffset: 0.001,
      basePrice: 3000000,
      area: 20,
      amenities: ['wifi', 'ac', 'heater'],
    },
    {
      uniIdx: 2,
      title: 'Chung cư mini Trần Thái Tông thang máy khóa vân tay',
      description: 'Full tiện nghi: điều hòa Inverter, nóng lạnh, hút mùi, kệ bếp.',
      address: 'Số 45 Trần Thái Tông, Dịch Vọng, Cầu Giấy, Hà Nội',
      latOffset: -0.004,
      lngOffset: 0.004,
      basePrice: 4200000,
      area: 26,
      amenities: ['wifi', 'ac', 'heater', 'parking'],
    },
    {
      uniIdx: 2,
      title: 'Phòng trọ sinh viên giá rẻ ngõ Hồ Tùng Mậu',
      description: 'Gần trạm xe buýt và ga metro Nhổn, thuận tiện đi lại.',
      address: 'Ngõ 20 Hồ Tùng Mậu, Mai Dịch, Cầu Giấy, Hà Nội',
      latOffset: 0.003,
      lngOffset: -0.005,
      basePrice: 2200000,
      area: 18,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 2,
      title: 'Studio ban công view thoáng đường Phạm Văn Đồng',
      description: 'Cửa sổ kính lớn, nhiều ánh sáng tự nhiên, máy giặt dùng chung tầng thượng.',
      address: 'Phạm Văn Đồng, Cổ Nhuế, Bắc Từ Liêm, Hà Nội',
      latOffset: 0.008,
      lngOffset: 0.002,
      basePrice: 3600000,
      area: 23,
      amenities: ['wifi', 'ac', 'heater', 'balcony'],
    },
    {
      uniIdx: 2,
      title: 'Phòng khép kín ngõ 336 Nguyễn Trãi gần VNU',
      description: 'Khu vực yên tĩnh, có chỗ nấu ăn riêng biệt, camera an ninh 24/7.',
      address: 'Ngõ Phan Văn Trường, Dịch Vọng Hậu, Cầu Giấy, Hà Nội',
      latOffset: -0.001,
      lngOffset: -0.002,
      basePrice: 2700000,
      area: 19,
      amenities: ['wifi', 'heater', 'parking'],
    },
    {
      uniIdx: 2,
      title: 'Căn hộ mini 1 phòng ngủ đường Duy Tân',
      description: 'Thích hợp cho sinh viên CNTT thích không gian riêng tư, yên tĩnh.',
      address: 'Duy Tân, Dịch Vọng Hậu, Cầu Giấy, Hà Nội',
      latOffset: -0.005,
      lngOffset: 0.001,
      basePrice: 4800000,
      area: 30,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },

    // 6 phòng quanh HCMUT (Quận 10, TP.HCM)
    {
      uniIdx: 3,
      title: 'Phòng trọ khép kín hẻm Tô Hiến Thành sát cổng trường BK',
      description: 'Đi bộ 2 phút là tới cổng Bách Khoa, phòng sạch sẽ, giờ giấc tự do.',
      address: 'Hẻm 280 Tô Hiến Thành, Phường 14, Quận 10, TP.HCM',
      latOffset: 0.002,
      lngOffset: 0.001,
      basePrice: 3300000,
      area: 20,
      amenities: ['wifi', 'ac', 'heater', 'parking'],
    },
    {
      uniIdx: 3,
      title: 'Phòng ban công thoáng mát hẻm Lý Thường Kiệt',
      description: 'Có máy lạnh, nệm cao su, máy nước nóng, cửa sổ đón gió cực thoáng.',
      address: 'Hẻm 354 Lý Thường Kiệt, Phường 14, Quận 10, TP.HCM',
      latOffset: -0.001,
      lngOffset: -0.002,
      basePrice: 3800000,
      area: 24,
      amenities: ['wifi', 'ac', 'heater', 'balcony'],
    },
    {
      uniIdx: 3,
      title: 'Ký túc xá cao cấp sleepbox đường Thành Thái',
      description: 'Mô hình sleepbox riêng tư, có rèm che, máy lạnh mở 24/24, bao điện nước.',
      address: 'Thành Thái, Phường 14, Quận 10, TP.HCM',
      latOffset: 0.004,
      lngOffset: 0.003,
      basePrice: 1900000,
      area: 35,
      amenities: ['wifi', 'ac', 'parking'],
    },
    {
      uniIdx: 3,
      title: 'Căn hộ mini full nội thất đường Nguyễn Tri Phương',
      description: 'Khu ẩm thực sầm uất, an ninh tuyệt đối, thang máy, bảo vệ thẻ từ.',
      address: 'Nguyễn Tri Phương, Phường 8, Quận 10, TP.HCM',
      latOffset: -0.005,
      lngOffset: 0.006,
      basePrice: 4700000,
      area: 28,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },
    {
      uniIdx: 3,
      title: 'Phòng trọ giá tốt cho sinh viên đường Ba Tháng Hai',
      description: 'Gần ngã tư Nguyễn Tri Phương - 3/2, gần siêu thị, trạm xe buýt.',
      address: 'Đường 3 Tháng 2, Phường 14, Quận 10, TP.HCM',
      latOffset: -0.003,
      lngOffset: 0.002,
      basePrice: 2900000,
      area: 20,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 3,
      title: 'Studio sang trọng view Landmark 81 đường Bắc Hải',
      description: 'Đầy đủ sofa, tivi thông minh, máy giặt riêng, bếp từ đôi hiện đại.',
      address: 'Đường Bắc Hải, Phường 15, Quận 10, TP.HCM',
      latOffset: 0.007,
      lngOffset: -0.001,
      basePrice: 5200000,
      area: 30,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },

    // 6 phòng quanh UIT (Thủ Đức, TP.HCM)
    {
      uniIdx: 4,
      title: 'Phòng trọ làng đại học ngõ chợ đêm Thủ Đức',
      description: 'Cách UIT 800m, gần hồ đá, không khí mát mẻ trong lành.',
      address: 'Khu phố 6, Linh Trung, TP. Thủ Đức, TP.HCM',
      latOffset: 0.002,
      lngOffset: -0.002,
      basePrice: 2100000,
      area: 20,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 4,
      title: 'Chung cư mini mới xây gần ĐH CNTT full đồ',
      description: 'Có máy lạnh, tủ lạnh mini, bếp nấu ăn, bảo vệ 24/7 an toàn.',
      address: 'Đường số 6, Linh Trung, TP. Thủ Đức, TP.HCM',
      latOffset: -0.003,
      lngOffset: 0.003,
      basePrice: 3200000,
      area: 25,
      amenities: ['wifi', 'ac', 'heater', 'parking'],
    },
    {
      uniIdx: 4,
      title: 'Phòng trọ có gác lửng đúc đường Quảng Trường Sáng Tạo',
      description: 'Gác cao đứng không đụng đầu, sàn gạch men sạch mát, giờ tự do.',
      address: 'Đường Quảng Trường Sáng Tạo, P. Đông Hòa, Dĩ An, Bình Dương',
      latOffset: 0.005,
      lngOffset: 0.002,
      basePrice: 2500000,
      area: 22,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 4,
      title: 'Căn hộ mini studio view hồ cá Koi ĐHQG',
      description: 'Phòng thiết kế hiện đại phong cách Bắc Âu, có ban công uống trà.',
      address: 'Lê Quý Đôn, Khu đô thị ĐHQG-HCM, Dĩ An, Bình Dương',
      latOffset: 0.003,
      lngOffset: -0.005,
      basePrice: 3900000,
      area: 28,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },
    {
      uniIdx: 4,
      title: 'Phòng trọ bình dân tiết kiệm đường Vành Đai ĐHQG',
      description: 'Phù hợp nhóm 2 bạn sinh viên IT cần phòng yên tĩnh code đêm.',
      address: 'Đường Vành Đai, Linh Trung, TP. Thủ Đức, TP.HCM',
      latOffset: -0.004,
      lngOffset: -0.003,
      basePrice: 1900000,
      area: 18,
      amenities: ['wifi', 'parking'],
    },
    {
      uniIdx: 4,
      title: 'Phòng cao cấp full nội thất khu cư xá ĐHQG',
      description: 'Trang bị bếp từ, máy hút mùi, máy giặt, thẻ từ thang máy riêng.',
      address: 'Khu phố Tân Lập, Đông Hòa, Dĩ An, Bình Dương',
      latOffset: 0.006,
      lngOffset: 0.004,
      basePrice: 4200000,
      area: 30,
      amenities: ['wifi', 'ac', 'heater', 'parking', 'balcony'],
    },
  ];

  const createdPosts = [];
  const now = new Date();
  const expiresAt = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000); // 30 ngày sau

  for (let i = 0; i < roomsSpec.length; i++) {
    const spec = roomsSpec[i];
    const hub = targetHubs[spec.uniIdx];
    const landlord = i % 2 === 0 ? landlord1 : landlord2;

    const post = await prisma.post.create({
      data: {
        userId: landlord.id,
        title: spec.title,
        description: spec.description,
        address: spec.address,
        latitude: parseFloat((hub.latitude + spec.latOffset).toFixed(6)),
        longitude: parseFloat((hub.longitude + spec.lngOffset).toFixed(6)),
        basePrice: spec.basePrice,
        electricPrice: 3800,
        waterPrice: 30000,
        internetPrice: 100000,
        otherFee: 50000,
        area: spec.area,
        maxPeople: spec.area > 25 ? 3 : 2,
        amenities: spec.amenities,
        images: [
          sampleImages[i % sampleImages.length],
          sampleImages[(i + 1) % sampleImages.length],
        ],
        status: 'AVAILABLE',
        expiresAt: expiresAt,
      },
    });
    createdPosts.push(post);
  }

  console.log('[SEED] Thêm đánh giá (Reviews) mẫu...');
  await prisma.review.create({
    data: {
      postId: createdPosts[0].id,
      userId: studentEdu.id,
      rating: 5,
      comment: 'Phòng cực kỳ thoáng và sạch sẽ, chủ nhà thân thiện. Rất gần trường đi bộ thoải mái!',
      isFlagged: false,
    },
  });

  await prisma.review.create({
    data: {
      postId: createdPosts[0].id,
      userId: studentNormal.id,
      rating: 4,
      comment: 'Giá cả hợp lý cho sinh viên, mạng ổn định để học trực tuyến.',
      isFlagged: false,
    },
  });

  await prisma.review.create({
    data: {
      postId: createdPosts[6].id,
      userId: studentEdu.id,
      rating: 5,
      comment: 'Gần NEU ăn uống quá tiện, an ninh tốt, xe cộ để an tâm.',
      isFlagged: false,
    },
  });

  console.log('[SEED] Hoàn tất seed dữ liệu thành công!');
}

main()
  .catch((e) => {
    console.error('[SEED] Lỗi khi seed dữ liệu:', e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
