$(document).ready(function() {
    let token = localStorage.getItem('token');

    if (token) {
        showAccountSection();
    }

    // Handle login
    $('#login-form').submit(function(event) {
        event.preventDefault();
        const username = $('#username').val();
        const password = $('#password').val();

        $.ajax({
            url: '/api/auth/login',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ username, password }),
            success: function(response) {
                localStorage.setItem('token', response.token); // เก็บ JWT Token ใหม่ใน localStorage
                token = response.token; // ใช้ Token ใหม่
                showAccountSection(); // แสดงส่วนบัญชีผู้ใช้
            },
            error: function() {
                $('#login-error').text('Invalid username or password');
            }
        });
    });

    // Handle register
    $('#register-form').submit(function(event) {
        event.preventDefault();
        const username = $('#register-username').val();
        const password = $('#register-password').val();

        $.ajax({
            url: '/api/auth/register',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ username, password }),
            success: function() {
                alert('Registration successful! Please log in.');
                showLoginSection(); // แสดงหน้า login หลังจากลงทะเบียนสำเร็จ
            },
            error: function(xhr) {
                // ตรวจสอบ responseJSON และดึงข้อความ error
                let errorMessage = 'Registration failed';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                alert(errorMessage); // แสดงข้อความ error ใน alert
            }
        });
    });

    // Handle logout
    $('#logout-btn').click(function() {
        localStorage.removeItem('token'); // ลบ JWT Token ออกจาก localStorage
        token = null; // รีเซ็ตค่า Token
        hideAccountSection(); // ซ่อนส่วนบัญชีผู้ใช้
    });

    // Handle deposit
    $('#deposit-form').submit(function(event) {
        event.preventDefault();
        const amount = $('#deposit-amount').val();
        const senderId = getSenderIdFromToken(); // ดึง senderId จาก token

        $.ajax({
            url: '/api/account/deposit',
            method: 'POST',
            headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
            contentType: 'application/json',
            data: JSON.stringify({ senderId, amount }),
            success: function(response) {
                $('#balance').text(response.balance); // อัปเดตยอดเงิน
                alert('Deposit successful!');
            },
            error: function() {
                alert('Deposit failed');
            }
        });
    });

    // Handle withdraw
    $('#withdraw-form').submit(function(event) {
        event.preventDefault();
        const amount = $('#withdraw-amount').val();
        const senderId = getSenderIdFromToken(); // ดึง senderId จาก token

        $.ajax({
            url: '/api/account/withdraw',
            method: 'POST',
            headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
            contentType: 'application/json',
            data: JSON.stringify({ senderId, amount }),
            success: function(response) {
                $('#balance').text(response.balance); // อัปเดตยอดเงิน
                alert('Withdraw successful!');
            },
            error: function() {
                alert('Withdraw failed');
            }
        });
    });

    // Handle transfer
    $('#transfer-form').submit(function(event) {
        event.preventDefault();
        const recipient = $('#transfer-username').val();
        const amount = $('#transfer-amount').val();
        const senderId = getSenderIdFromToken(); // ดึง senderId จาก token
    
        // ตรวจสอบว่าไม่สามารถโอนเงินให้ตัวเองได้
        if (recipient == senderId) {
            alert("You cannot transfer money to yourself.");
            return; // หยุดการทำงานที่นี่และไม่ส่งคำขอไปที่ API
        }
    
        $.ajax({
            url: '/api/account/transfer',
            method: 'POST',
            headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
            contentType: 'application/json',
            data: JSON.stringify({ senderId, receiverId: parseInt(recipient), amount: parseFloat(amount) }),
            success: function(response) {
                $('#balance').text(response.senderBalance); // อัปเดตยอดเงินของผู้ส่ง
                alert('Transfer successful!');
            },
            error: function() {
                alert('Transfer failed');
            }
        });
    });
    

// Fetch balance, user profile, and transaction history
function showAccountSection() {
    $('#login-section').addClass('d-none');
    $('#account-section').removeClass('d-none');

    // Fetch user profile (Username and ID)
    $.ajax({
        url: '/api/account/profile',
        method: 'GET',
        headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
        success: function(response) {
            // ตรวจสอบว่า response มีค่า Username และ UserId หรือไม่
            if (response && response.username && response.userId) {
                // แสดงชื่อและ ID ของผู้ใช้
                $('#account-username').text(response.username);
                $('#account-id').text(response.userId);
            } else {
                alert('Failed to load user profile. Missing data.');
            }
        },
        error: function() {
            alert('Failed to load user profile.');
        }
    });

    // Fetch balance
    $.ajax({
        url: '/api/account/balance',
        method: 'GET',
        headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
        success: function(response) {
            $('#balance').text(response.balance); // แสดงยอดเงิน
        },
        error: function() {
            alert('Failed to load balance.');
        }
    });

    // Fetch transactions
    $.ajax({
        url: '/api/account/history',
        method: 'GET',
        headers: { Authorization: 'Bearer ' + token }, // ส่ง Token ปัจจุบันไปกับคำขอ
        success: function(transactions) {
            let tableBody = '';
            transactions.forEach(function(transaction) {
                const formattedDate = new Date(transaction.timestamp).toLocaleString();
                let typeClass = '';
                let typeIcon = '';
                let amountSign = '';
                let transactionParty = '';

                if (transaction.transactionType === 'Deposit') {
                    typeClass = 'text-success';
                    typeIcon = '⬆️';
                    amountSign = '+';
                    transactionParty = `From: Bank`;
                } else if (transaction.transactionType === 'Withdraw') {
                    typeClass = 'text-danger';
                    typeIcon = '⬇️';
                    amountSign = '-';
                    transactionParty = `To: Bank`;
                } else if (transaction.transactionType === 'Transfer') {
                    typeClass = 'text-warning';
                    typeIcon = '🔄';
                    if (transaction.senderId == getSenderIdFromToken()) {
                        amountSign = '-';
                        transactionParty = `To: User ${transaction.receiverId}`;
                    } else {
                        amountSign = '+';
                        transactionParty = `From: User ${transaction.senderId}`;
                    }
                }

                tableBody += `
                    <tr>
                        <td>${formattedDate}</td>
                        <td class="${typeClass}">${typeIcon} ${transaction.transactionType}</td>
                        <td>${amountSign}${transaction.amount.toFixed(2)}</td>
                        <td>${transactionParty}</td>
                    </tr>
                `;
            });
            $('#transaction-history tbody').html(tableBody);
        },
        error: function() {
            alert('Failed to load transaction history.');
        }
    });

    }

    function hideAccountSection() {
        $('#login-section').removeClass('d-none');
        $('#account-section').addClass('d-none');
    }

    // Function to get senderId from JWT token
    function getSenderIdFromToken() {
        const payload = JSON.parse(atob(token.split('.')[1])); // ถอดรหัส payload ของ JWT token
        return payload.nameid; // senderId ถูกเก็บใน claim 'nameid'
    }

    // Switch between register and login sections
    $('#show-register-btn').click(function() {
        $('#login-section').addClass('d-none');
        $('#register-section').removeClass('d-none');
    });

    $('#show-login-btn').click(function() {
        $('#register-section').addClass('d-none');
        $('#login-section').removeClass('d-none');
    });

    function showLoginSection() {
        $('#register-section').addClass('d-none');
        $('#login-section').removeClass('d-none');
    }
});
